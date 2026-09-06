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
        /// Registra un abono validando el saldo dentro de la misma transacción.
        /// Esto evita que dos registros simultáneos puedan sobrepasar el costo
        /// del tratamiento sin requerir cambios en la estructura de la base de datos.
        /// </summary>
        public async Task<(bool Registrado, decimal SaldoAntes)> RegistrarPagoValidadoAsync(PagoModel pago)
        {
            if (pago.Monto <= 0)
                return (false, 0m);

            using IDbConnection db = DatabaseConnection.GetConnection();
            db.Open();

            using IDbTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                const string saldoSql = @"
SELECT
    CAST(tp.CostoTotal - ISNULL(SUM(pg.Monto), 0) AS DECIMAL(10,2)) AS SaldoPendiente
FROM Odontologia.TratamientosPaciente tp WITH (UPDLOCK, HOLDLOCK)
LEFT JOIN Odontologia.Pagos pg WITH (UPDLOCK, HOLDLOCK)
    ON pg.IdTratamientoPaciente = tp.Id
WHERE tp.Id = @IdTratamientoPaciente
GROUP BY tp.CostoTotal;";

                decimal? saldo = await db.QuerySingleOrDefaultAsync<decimal?>(
                    saldoSql,
                    new { pago.IdTratamientoPaciente },
                    transaction);

                if (!saldo.HasValue)
                    throw new InvalidOperationException("No se encontró el tratamiento asociado al pago.");

                decimal saldoActual = Math.Max(0m, saldo.Value);

                if (saldoActual <= 0m || pago.Monto > saldoActual)
                {
                    transaction.Rollback();
                    return (false, saldoActual);
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
                return (true, saldoActual);
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // La excepción original es la que debe propagarse.
                }

                throw;
            }
        }

        public async Task<decimal> ObtenerSaldoPendienteAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdTratamientoPaciente = idTratamientoPaciente };

            return await db.ExecuteScalarAsync<decimal>(
                "Odontologia.sp_SaldoPendiente",
                parameters,
                commandType: CommandType.StoredProcedure);
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
    }
}
