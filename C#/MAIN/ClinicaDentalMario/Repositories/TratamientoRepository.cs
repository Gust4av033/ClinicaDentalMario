using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class TratamientoRepository
    {
        public async Task<IEnumerable<TratamientoPacienteModel>> ObtenerPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT tp.*, ct.Nombre AS NombreTratamiento
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Catalogos.CatalogoTratamientos ct ON tp.IdTratamiento = ct.IdTratamiento
                WHERE tp.IdPaciente = @IdPaciente";

            return await db.QueryAsync<TratamientoPacienteModel>(sql, new { IdPaciente = idPaciente });
        }

        public async Task CrearTratamientoAsync(TratamientoPacienteModel tratamiento)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                tratamiento.IdPaciente,
                tratamiento.IdDoctor,
                tratamiento.IdTratamiento,
                tratamiento.CostoTotal,
                tratamiento.Observaciones
            };

            await db.ExecuteAsync("Odontologia.sp_CrearTratamiento", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task FinalizarTratamientoAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdTratamientoPaciente = idTratamientoPaciente };
            await db.ExecuteAsync("Odontologia.sp_FinalizarTratamiento", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int?> ObtenerIdTratamientoActivoAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT TOP 1 Id
                FROM Odontologia.TratamientosPaciente
                WHERE IdPaciente = @IdPaciente AND Estado = 'En Progreso'
                ORDER BY FechaInicio DESC";

            return await db.QueryFirstOrDefaultAsync<int?>(sql, new { IdPaciente = idPaciente });
        }

        public async Task ActualizarTratamientoAsync(int idTratamientoPaciente, decimal costoTotal, string observaciones)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                UPDATE Odontologia.TratamientosPaciente
                SET CostoTotal = @CostoTotal,
                    Observaciones = @Observaciones
                WHERE Id = @IdTratamientoPaciente";

            await db.ExecuteAsync(sql, new
            {
                CostoTotal = costoTotal,
                Observaciones = observaciones,
                IdTratamientoPaciente = idTratamientoPaciente
            });
        }

        public async Task<IEnumerable<dynamic>> ObtenerProductividadAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT
                    t.Nombre AS Tratamiento,
                    COUNT(tp.Id) AS Cantidad,
                    SUM(tp.CostoTotal) AS IngresoProyectado
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Catalogos.CatalogoTratamientos t ON tp.IdTratamiento = t.IdTratamiento
                WHERE tp.FechaInicio >= @Inicio AND tp.FechaInicio < @Fin
                GROUP BY t.Nombre
                ORDER BY Cantidad DESC";

            return await db.QueryAsync<dynamic>(sql, new { Inicio = fechaInicio, Fin = fechaFin.AddDays(1) });
        }
    }
}
