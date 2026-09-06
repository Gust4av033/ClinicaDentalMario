using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class PagoRepository
    {
        public async Task<IEnumerable<PagoModel>> ListarPagosAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdTratamientoPaciente = idTratamientoPaciente };

            return await db.QueryAsync<PagoModel>(
                "Odontologia.sp_ListarPagos",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task RegistrarPagoAsync(PagoModel pago)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                pago.IdTratamientoPaciente,
                pago.Monto,
                pago.MetodoPago,
                pago.Observacion
            };

            await db.ExecuteAsync(
                "Odontologia.sp_RegistrarPago",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Registra un abono validando dentro de la misma transacción tanto el saldo
        /// como el estado actual del tratamiento. No requiere cambios en la BD.
        /// Un tratamiento finalizado o cancelado se considera cerrado y sin saldo cobrable.
        /// </summary>
        public async Task<(bool Registrado, decimal SaldoAntes, string EstadoTratamiento)> RegistrarPagoValidadoAsync(PagoModel pago)
        {
            if (pago.Monto <= 0)
                return (false, 0m, string.Empty);

            using IDbConnection db = DatabaseConnection.GetConnection();
            db.Open();

            using IDbTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                const string estadoSaldoSql = @"
SELECT
    CAST(tp.CostoTotal - ISNULL(SUM(pg.Monto), 0) AS DECIMAL(10,2)) AS SaldoPendiente,
    tp.Estado
FROM Odontologia.TratamientosPaciente tp WITH (UPDLOCK, HOLDLOCK)
LEFT JOIN Odontologia.Pagos pg WITH (UPDLOCK, HOLDLOCK)
    ON pg.IdTratamientoPaciente = tp.Id
WHERE tp.Id = @IdTratamientoPaciente
GROUP BY tp.CostoTotal, tp.Estado;";

                var datos = await db.QuerySingleOrDefaultAsync<EstadoSaldoTratamientoRow>(
                    estadoSaldoSql,
                    new { pago.IdTratamientoPaciente },
                    transaction);

                if (datos is null)
                    throw new InvalidOperationException("No se encontró el tratamiento asociado al pago.");

                string estadoActual = datos.Estado ?? string.Empty;
                bool estadoPermitePago =
                    string.Equals(estadoActual, "Pendiente", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(estadoActual, "En progreso", StringComparison.OrdinalIgnoreCase);

                decimal saldoActual = estadoPermitePago
                    ? Math.Max(0m, datos.SaldoPendiente)
                    : 0m;

                if (!estadoPermitePago || saldoActual <= 0m || pago.Monto > saldoActual)
                {
                    transaction.Rollback();
                    return (false, saldoActual, estadoActual);
                }

                var parameters = new
                {
                    pago.IdTratamientoPaciente,
                    pago.Monto,
                    pago.MetodoPago,
                    pago.Observacion
                };

                await db.ExecuteAsync(
                    "Odontologia.sp_RegistrarPago",
                    parameters,
                    transaction,
                    commandType: CommandType.StoredProcedure);

                transaction.Commit();
                return (true, saldoActual, estadoActual);
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // Se conserva la excepción original.
                }

                throw;
            }
        }

        /// <summary>
        /// Devuelve el saldo actualmente cobrable. Pendiente y En progreso pueden tener saldo;
        /// Finalizado y Cancelado se consideran cerrados y devuelven cero.
        /// La regla se aplica en C# sin modificar funciones ni procedimientos de la BD instalada.
        /// </summary>
        public async Task<decimal> ObtenerSaldoPendienteAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
SELECT CAST(
    CASE
        WHEN tp.Estado IN ('Pendiente', 'En progreso')
            THEN tp.CostoTotal - ISNULL(SUM(pg.Monto), 0)
        ELSE 0
    END
AS DECIMAL(10,2))
FROM Odontologia.TratamientosPaciente tp
LEFT JOIN Odontologia.Pagos pg
    ON pg.IdTratamientoPaciente = tp.Id
WHERE tp.Id = @IdTratamientoPaciente
GROUP BY tp.CostoTotal, tp.Estado;";

            decimal? saldo = await db.QuerySingleOrDefaultAsync<decimal?>(
                sql,
                new { IdTratamientoPaciente = idTratamientoPaciente });

            return Math.Max(0m, saldo ?? 0m);
        }

        public async Task<IEnumerable<PagoModel>> ListarPagosPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
SELECT pg.*
FROM Odontologia.Pagos pg
INNER JOIN Odontologia.TratamientosPaciente tp
    ON pg.IdTratamientoPaciente = tp.Id
WHERE tp.IdPaciente = @IdPaciente
ORDER BY pg.FechaPago DESC;";

            return await db.QueryAsync<PagoModel>(sql, new { IdPaciente = idPaciente });
        }

        /// <summary>
        /// Obtiene en una sola consulta el total abonado por cada tratamiento
        /// del paciente. Reemplaza el patrón N+1 utilizado por la vista global.
        /// </summary>
        public async Task<Dictionary<int, decimal>> ObtenerTotalesPagadosPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
SELECT
    tp.Id AS IdTratamientoPaciente,
    CAST(ISNULL(SUM(pg.Monto), 0) AS DECIMAL(10,2)) AS TotalPagado
FROM Odontologia.TratamientosPaciente tp
LEFT JOIN Odontologia.Pagos pg
    ON pg.IdTratamientoPaciente = tp.Id
WHERE tp.IdPaciente = @IdPaciente
GROUP BY tp.Id;";

            var filas = await db.QueryAsync<PagoTotalTratamientoRow>(
                sql,
                new { IdPaciente = idPaciente });

            return filas.ToDictionary(x => x.IdTratamientoPaciente, x => x.TotalPagado);
        }

        public async Task<IEnumerable<dynamic>> ObtenerIngresosPorRangoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
SELECT
    p.IdPago,
    p.FechaPago,
    p.Monto,
    p.MetodoPago,
    p.Observacion,
    pac.NombreCompleto AS Paciente,
    t.Nombre AS Tratamiento
FROM Odontologia.Pagos p
INNER JOIN Odontologia.TratamientosPaciente tp ON p.IdTratamientoPaciente = tp.Id
INNER JOIN Catalogos.CatalogoTratamientos t ON tp.IdTratamiento = t.IdTratamiento
INNER JOIN Pacientes.Pacientes pac ON tp.IdPaciente = pac.IdPaciente
WHERE p.FechaPago >= @Inicio AND p.FechaPago < @Fin
ORDER BY p.FechaPago ASC;";

            return await db.QueryAsync<dynamic>(
                sql,
                new { Inicio = fechaInicio, Fin = fechaFin.AddDays(1) });
        }

        private sealed class PagoTotalTratamientoRow
        {
            public int IdTratamientoPaciente { get; set; }
            public decimal TotalPagado { get; set; }
        }

        private sealed class EstadoSaldoTratamientoRow
        {
            public decimal SaldoPendiente { get; set; }
            public string? Estado { get; set; }
        }
    }
}
