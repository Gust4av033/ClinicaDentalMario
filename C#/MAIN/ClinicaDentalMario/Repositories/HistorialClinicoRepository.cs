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

            const string sql = @"
                SELECT
                    hc.IdHistorial,
                    hc.IdPaciente,
                    hc.IdDoctor,
                    d.NombreCompleto AS Doctor,
                    hc.MotivoConsulta,
                    hc.AntecedentesMedicos,
                    hc.AntecedentesOdontologicos,
                    hc.Diagnostico,
                    hc.PlanTratamiento,
                    hc.Observaciones,
                    hc.FechaConsulta
                FROM Pacientes.HistorialClinico hc
                INNER JOIN Personal.Doctores d ON d.IdDoctor = hc.IdDoctor
                WHERE hc.IdPaciente = @IdPaciente
                ORDER BY hc.FechaConsulta DESC;";

            return await db.QueryAsync<HistorialClinicoModel>(
                sql,
                new { IdPaciente = idPaciente });
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

            await db.ExecuteAsync(
                "Pacientes.sp_InsertarConsulta",
                parameters,
                commandType: CommandType.StoredProcedure);
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

            await db.ExecuteAsync(
                "Pacientes.sp_EditarConsulta",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<HistorialClinicoModel?> ObtenerPorIdPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT TOP 1
                    hc.IdHistorial,
                    hc.IdPaciente,
                    hc.IdDoctor,
                    d.NombreCompleto AS Doctor,
                    hc.MotivoConsulta,
                    hc.AntecedentesMedicos,
                    hc.AntecedentesOdontologicos,
                    hc.Diagnostico,
                    hc.PlanTratamiento,
                    hc.Observaciones,
                    hc.FechaConsulta
                FROM Pacientes.HistorialClinico hc
                INNER JOIN Personal.Doctores d ON d.IdDoctor = hc.IdDoctor
                WHERE hc.IdPaciente = @IdPaciente
                ORDER BY hc.FechaConsulta DESC;";

            return await db.QueryFirstOrDefaultAsync<HistorialClinicoModel>(
                sql,
                new { IdPaciente = idPaciente });
        }

        public Task<IEnumerable<HistorialClinicoModel>> ObtenerHistorialPorPacienteAsync(int idPaciente)
        {
            return ListarConsultasAsync(idPaciente);
        }
    }
}
