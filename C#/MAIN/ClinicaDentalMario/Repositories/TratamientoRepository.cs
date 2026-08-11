using ClinicaDentalMario.Config;
using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Repositories
{
    public class TratamientoRepository
    {
        private readonly string _connectionString;

        // Constructor por defecto: usa la conexión oficial
        public TratamientoRepository()
        {
            _connectionString = AppSettings.ConnectionString;
        }

        // Constructor alternativo: si quieres pasar otro connectionString manual
        public TratamientoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<TratamientoPacienteModel>> ObtenerPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT tp.*, ct.Nombre AS NombreTratamiento
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Catalogos.CatalogoTratamientos ct ON tp.IdTratamiento = ct.IdTratamiento
                WHERE tp.IdPaciente = @IdPaciente";

            // Aquí le decimos a Dapper exactamente qué tipo devolver
            return await db.QueryAsync<TratamientoPacienteModel>(sql, new { IdPaciente = idPaciente });
        }

        public async Task CrearTratamientoAsync(TratamientoPacienteModel tratamiento)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
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
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { IdTratamientoPaciente = idTratamientoPaciente };
            await db.ExecuteAsync("Odontologia.sp_FinalizarTratamiento", parameters, commandType: CommandType.StoredProcedure);
        }

        // EL NUEVO MÉTODO PARA BUSCAR EL TRATAMIENTO ACTIVO 🔥
        public async Task<int?> ObtenerIdTratamientoActivoAsync(int idPaciente)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = @"
                SELECT TOP 1 Id 
                FROM Odontologia.TratamientosPaciente 
                WHERE IdPaciente = @IdPaciente AND Estado = 'En Progreso' 
                ORDER BY FechaInicio DESC";

            return await db.QueryFirstOrDefaultAsync<int?>(sql, new { IdPaciente = idPaciente });
        }
    }
}