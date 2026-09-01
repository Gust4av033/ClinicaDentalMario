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
                    c.Observaciones,
                    p.NombreCompleto AS Paciente,
                    d.NombreCompleto AS Doctor,
                    e.Nombre AS Estado
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON c.IdPaciente = p.IdPaciente
                INNER JOIN Personal.Doctores d ON c.IdDoctor = d.IdDoctor
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE CAST(c.FechaHora AS DATE) = CAST(@Fecha AS DATE)
                  AND e.Nombre <> 'Cancelada'
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

        public async Task<bool> ExisteConflictoDoctorAsync(
            int idDoctor,
            DateTime fechaHora,
            int? excluirIdCita = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT COUNT(1)
                FROM Agenda.Citas c
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE c.IdDoctor = @IdDoctor
                  AND c.FechaHora = @FechaHora
                  AND e.Nombre NOT IN ('Cancelada', 'No Asistió')
                  AND (@ExcluirIdCita IS NULL OR c.IdCita <> @ExcluirIdCita);";

            int cantidad = await db.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    IdDoctor = idDoctor,
                    FechaHora = fechaHora,
                    ExcluirIdCita = excluirIdCita
                });

            return cantidad > 0;
        }

        public async Task InsertarAsync(CitaModel cita)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                cita.IdPaciente,
                cita.IdDoctor,
                cita.IdEstado,
                cita.FechaHora,
                cita.Observaciones
            };

            await db.ExecuteAsync(
                "Agenda.sp_AgendarCita",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarCitaAsync(
            int idCita,
            int idDoctor,
            int idEstado,
            DateTime fechaHora,
            string? observaciones)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                UPDATE Agenda.Citas
                SET IdDoctor = @IdDoctor,
                    IdEstado = @IdEstado,
                    FechaHora = @FechaHora,
                    Observaciones = @Observaciones
                WHERE IdCita = @IdCita;";

            await db.ExecuteAsync(
                sql,
                new
                {
                    IdCita = idCita,
                    IdDoctor = idDoctor,
                    IdEstado = idEstado,
                    FechaHora = fechaHora,
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

            await db.ExecuteAsync(
                sql,
                new { IdCita = idCita, NombreEstado = nombreEstado });
        }
    }
}
