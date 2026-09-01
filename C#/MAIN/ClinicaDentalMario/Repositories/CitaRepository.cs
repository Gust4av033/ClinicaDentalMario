using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class CitaRepository
    {
        public async Task<IEnumerable<AgendaCitaModel>> ObtenerCitasPorFechaAsync(DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT
                    c.IdCita,
                    c.IdPaciente,
                    c.IdDoctor,
                    c.IdEstado,
                    c.FechaHora,
                    c.DuracionMinutos,
                    c.Observaciones,
                    p.NombreCompleto AS Paciente,
                    d.NombreCompleto AS Doctor,
                    e.Nombre AS Estado
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON c.IdPaciente = p.IdPaciente
                INNER JOIN Personal.Doctores d ON c.IdDoctor = d.IdDoctor
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE CAST(c.FechaHora AS DATE) = CAST(@Fecha AS DATE)
                ORDER BY c.FechaHora ASC;";

            return await db.QueryAsync<AgendaCitaModel>(sql, new { Fecha = fecha.Date });
        }

        public async Task<IEnumerable<EstadoCitaModel>> ObtenerEstadosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT IdEstado, Nombre
                FROM Catalogos.EstadosCita
                ORDER BY IdEstado;";

            return await db.QueryAsync<EstadoCitaModel>(sql);
        }

        public async Task<int?> ObtenerIdEstadoAsync(string nombreEstado)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT TOP 1 IdEstado
                FROM Catalogos.EstadosCita
                WHERE Nombre = @NombreEstado;";

            return await db.QuerySingleOrDefaultAsync<int?>(
                sql,
                new { NombreEstado = nombreEstado });
        }

        public Task<bool> ExisteConflictoDoctorAsync(
            int idDoctor,
            DateTime fechaHoraInicio,
            int duracionMinutos,
            int? excluirIdCita = null)
        {
            return ExisteConflictoAsync(
                "c.IdDoctor = @IdEntidad",
                idDoctor,
                fechaHoraInicio,
                duracionMinutos,
                excluirIdCita);
        }

        public Task<bool> ExisteConflictoPacienteAsync(
            int idPaciente,
            DateTime fechaHoraInicio,
            int duracionMinutos,
            int? excluirIdCita = null)
        {
            return ExisteConflictoAsync(
                "c.IdPaciente = @IdEntidad",
                idPaciente,
                fechaHoraInicio,
                duracionMinutos,
                excluirIdCita);
        }

        private static async Task<bool> ExisteConflictoAsync(
            string filtroEntidad,
            int idEntidad,
            DateTime fechaHoraInicio,
            int duracionMinutos,
            int? excluirIdCita)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            DateTime fechaHoraFin = fechaHoraInicio.AddMinutes(duracionMinutos);

            string sql = $@"
                SELECT COUNT(1)
                FROM Agenda.Citas c
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE {filtroEntidad}
                  AND e.Nombre NOT IN ('Cancelada', 'No Asistió')
                  AND (@ExcluirIdCita IS NULL OR c.IdCita <> @ExcluirIdCita)
                  AND c.FechaHora < @FechaHoraFin
                  AND DATEADD(MINUTE, c.DuracionMinutos, c.FechaHora) > @FechaHoraInicio;";

            int cantidad = await db.ExecuteScalarAsync<int>(sql, new
            {
                IdEntidad = idEntidad,
                FechaHoraInicio = fechaHoraInicio,
                FechaHoraFin = fechaHoraFin,
                ExcluirIdCita = excluirIdCita
            });

            return cantidad > 0;
        }

        public async Task InsertarAsync(CitaModel cita)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                INSERT INTO Agenda.Citas
                    (IdPaciente, IdDoctor, IdEstado, FechaHora, DuracionMinutos, Observaciones)
                VALUES
                    (@IdPaciente, @IdDoctor, @IdEstado, @FechaHora, @DuracionMinutos, @Observaciones);";

            await db.ExecuteAsync(sql, new
            {
                cita.IdPaciente,
                cita.IdDoctor,
                cita.IdEstado,
                cita.FechaHora,
                cita.DuracionMinutos,
                Observaciones = string.IsNullOrWhiteSpace(cita.Observaciones)
                    ? null
                    : cita.Observaciones.Trim()
            });
        }

        public async Task ActualizarCitaAsync(
            int idCita,
            int idDoctor,
            int idEstado,
            DateTime fechaHora,
            int duracionMinutos,
            string? observaciones)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                UPDATE Agenda.Citas
                SET IdDoctor = @IdDoctor,
                    IdEstado = @IdEstado,
                    FechaHora = @FechaHora,
                    DuracionMinutos = @DuracionMinutos,
                    Observaciones = @Observaciones
                WHERE IdCita = @IdCita;";

            await db.ExecuteAsync(sql, new
            {
                IdCita = idCita,
                IdDoctor = idDoctor,
                IdEstado = idEstado,
                FechaHora = fechaHora,
                DuracionMinutos = duracionMinutos,
                Observaciones = string.IsNullOrWhiteSpace(observaciones)
                    ? null
                    : observaciones.Trim()
            });
        }

        public async Task CancelarCitaAsync(int idCita)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            await db.ExecuteAsync(
                "Agenda.sp_CancelarCita",
                new { IdCita = idCita },
                commandType: CommandType.StoredProcedure);
        }

        public async Task CambiarEstadoCitaAsync(int idCita, string nombreEstado)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                UPDATE Agenda.Citas
                SET IdEstado = (
                    SELECT TOP 1 IdEstado
                    FROM Catalogos.EstadosCita
                    WHERE Nombre = @NombreEstado)
                WHERE IdCita = @IdCita;";

            await db.ExecuteAsync(sql, new { IdCita = idCita, NombreEstado = nombreEstado });
        }
    }
}
