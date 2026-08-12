using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class HistorialClinicoRepository
    {
        public async Task<IEnumerable<HistorialClinicoModel>> ListarConsultasAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };
            //[cite_start]// Llama al SP que lee de la vista vwHistorialPaciente [cite: 765, 832]
            return await db.QueryAsync<HistorialClinicoModel>("Pacientes.sp_ListarConsultasPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task InsertarConsultaAsync(HistorialClinicoModel historial)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                historial.IdPaciente,
                historial.IdDoctor,
                historial.MotivoConsulta,
                historial.AntecedentesMedicos,
                historial.AntecedentesOdontologicos,
                historial.Diagnostico,
                historial.PlanTratamiento,
                historial.Observaciones
            };
            //[cite_start]// SP para guardar el récord médico [cite: 761, 831]
            await db.ExecuteAsync("Pacientes.sp_InsertarConsulta", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task EditarConsultaAsync(HistorialClinicoModel historial)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                historial.IdHistorial,
                historial.MotivoConsulta,
                historial.AntecedentesMedicos,
                historial.AntecedentesOdontologicos
            };

            await db.ExecuteAsync("Pacientes.sp_EditarConsulta", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<HistorialClinicoModel?> ObtenerPorIdPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };

            // Usamos el procedimiento almacenado que ya usas para listar o una consulta directa al esquema
            string sql = "SELECT TOP 1 * FROM Pacientes.HistorialClinico WHERE IdPaciente = @IdPaciente ORDER BY FechaConsulta DESC";

            return await db.QueryFirstOrDefaultAsync<HistorialClinicoModel>(sql, parameters);
        }
        public async Task<IEnumerable<HistorialClinicoModel>> ObtenerHistorialPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };

            // Consulta directa o usando tu procedimiento almacenado de consultas
            string sql = "SELECT * FROM Pacientes.HistorialClinico WHERE IdPaciente = @IdPaciente ORDER BY FechaConsulta DESC";

            return await db.QueryAsync<HistorialClinicoModel>(sql, parameters);
        }


    }
}
