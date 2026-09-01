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
            return await db.QueryAsync<PagoModel>("Odontologia.sp_ListarPagos", parameters, commandType: CommandType.StoredProcedure);
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
            await db.ExecuteAsync("Odontologia.sp_RegistrarPago", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<decimal> ObtenerSaldoPendienteAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdTratamientoPaciente = idTratamientoPaciente };
            // Ejecuta el SP que llama internamente a la función escalar
            return await db.ExecuteScalarAsync<decimal>("Odontologia.sp_SaldoPendiente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PagoModel>> ListarPagosPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };

            // CORRECCIÓN: Se cambió "TratamientoPaciente" por "TratamientosPaciente" y se ajustó el "SELECT Id"
            string sql = "SELECT * FROM Odontologia.Pagos WHERE IdTratamientoPaciente IN (SELECT Id FROM Odontologia.TratamientosPaciente WHERE IdPaciente = @IdPaciente)";

            return await db.QueryAsync<PagoModel>(sql, parameters);
        }
        // 🔥 MÉTODOS PARA ELIMINAR Y OBTENER DETALLE DE PAGO 🔥
        public async Task EliminarPagoAsync(int idPago)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = "DELETE FROM Odontologia.Pagos WHERE IdPago = @IdPago";
            await db.ExecuteAsync(sql, new { IdPago = idPago });
        }

        public async Task<IEnumerable<dynamic>> ObtenerIngresosPorRangoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            string sql = @"
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
        ORDER BY p.FechaPago ASC";

            return await db.QueryAsync<dynamic>(sql, new { Inicio = fechaInicio, Fin = fechaFin.AddDays(1) });
        }
    }
}
