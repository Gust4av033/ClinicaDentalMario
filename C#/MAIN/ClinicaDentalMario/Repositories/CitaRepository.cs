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
            return await ObtenerCitasAsync(
                fecha.Date,
                fecha.Date.AddDays(1));
        }

        public async Task<IEnumerable<AgendaCitaModel>> ObtenerCitasAsync(
            DateTime? fechaDesde = null,
            DateTime? fechaHastaExclusiva = null,
            int? idDoctor = null,
            int? idEstado = null,
            string? termino = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            bool tieneDuracion = await TieneDuracionMinutosAsync(db);

            string columnaDuracion = tieneDuracion
                ? "c.DuracionMinutos"
                : "30 AS DuracionMinutos";

            string sql = $@"
                SELECT
                    c.IdCita,
                    c.IdPaciente,
                    c.IdDoctor,
                    c.IdEstado,
                    c.FechaHora,
                    {columnaDuracion},
                    c.Observaciones,
                    p.NombreCompleto AS Paciente,
                    p.Telefono AS TelefonoPaciente,
                    d.NombreCompleto AS Doctor,
                    e.Nombre AS Estado
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON c.IdPaciente = p.IdPaciente
                INNER JOIN Personal.Doctores d ON c.IdDoctor = d.IdDoctor
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE (@FechaDesde IS NULL OR c.FechaHora >= @FechaDesde)
                  AND (@FechaHasta IS NULL OR c.FechaHora < @FechaHasta)
                  AND (@IdDoctor IS NULL OR c.IdDoctor = @IdDoctor)
                  AND (@IdEstado IS NULL OR c.IdEstado = @IdEstado)
                  AND (
                        @Termino IS NULL
                        OR p.NombreCompleto LIKE '%' + @Termino + '%'
                        OR p.Telefono LIKE '%' + @Termino + '%'
                        OR p.DUI LIKE '%' + @Termino + '%'
                      )
                ORDER BY c.FechaHora ASC;";

            string? terminoNormalizado = string.IsNullOrWhiteSpace(termino)
                ? null
                : termino.Trim();

            return await db.QueryAsync<AgendaCitaModel>(sql, new
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHastaExclusiva,
                IdDoctor = idDoctor,
                IdEstado = idEstado,
                Termino = terminoNormalizado
            });
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

            return await db.QuerySingleOrDefaultAsync<int?>(sql, new { NombreEstado = nombreEstado });
        }

        public async Task<bool> SoportaDuracionPersonalizadaAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            return await TieneDuracionMinutosAsync(db);
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
            bool tieneDuracion = await TieneDuracionMinutosAsync(db);
            DateTime fechaHoraFin = fechaHoraInicio.AddMinutes(duracionMinutos);

            string expresionFinCita = tieneDuracion
                ? "DATEADD(MINUTE, c.DuracionMinutos, c.FechaHora)"
                : "DATEADD(MINUTE, 30, c.FechaHora)";

            string sql = $@"
                SELECT COUNT(1)
                FROM Agenda.Citas c
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE {filtroEntidad}
                  AND e.Nombre NOT IN ('Atendida', 'Cancelada', 'No Asistió')
                  AND (@ExcluirIdCita IS NULL OR c.IdCita <> @ExcluirIdCita)
                  AND c.FechaHora < @FechaHoraFin
                  AND {expresionFinCita} > @FechaHoraInicio;";

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
            bool tieneDuracion = await TieneDuracionMinutosAsync(db);

            string sql = tieneDuracion
                ? @"
                    INSERT INTO Agenda.Citas
                        (IdPaciente, IdDoctor, IdEstado, FechaHora, DuracionMinutos, Observaciones)
                    VALUES
                        (@IdPaciente, @IdDoctor, @IdEstado, @FechaHora, @DuracionMinutos, @Observaciones);"
                : @"
                    INSERT INTO Agenda.Citas
                        (IdPaciente, IdDoctor, IdEstado, FechaHora, Observaciones)
                    VALUES
                        (@IdPaciente, @IdDoctor, @IdEstado, @FechaHora, @Observaciones);";

            await db.ExecuteAsync(sql, new
            {
                cita.IdPaciente,
                cita.IdDoctor,
                cita.IdEstado,
                cita.FechaHora,
                DuracionMinutos = tieneDuracion ? cita.DuracionMinutos : 30,
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
            bool tieneDuracion = await TieneDuracionMinutosAsync(db);

            string sql = tieneDuracion
                ? @"
                    UPDATE Agenda.Citas
                    SET IdDoctor = @IdDoctor,
                        IdEstado = @IdEstado,
                        FechaHora = @FechaHora,
                        DuracionMinutos = @DuracionMinutos,
                        Observaciones = @Observaciones
                    WHERE IdCita = @IdCita;"
                : @"
                    UPDATE Agenda.Citas
                    SET IdDoctor = @IdDoctor,
                        IdEstado = @IdEstado,
                        FechaHora = @FechaHora,
                        Observaciones = @Observaciones
                    WHERE IdCita = @IdCita;";

            await db.ExecuteAsync(sql, new
            {
                IdCita = idCita,
                IdDoctor = idDoctor,
                IdEstado = idEstado,
                FechaHora = fechaHora,
                DuracionMinutos = tieneDuracion ? duracionMinutos : 30,
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
            if (string.IsNullOrWhiteSpace(nombreEstado))
                throw new ArgumentException("Debe indicar un estado de cita válido.", nameof(nombreEstado));

            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sqlEstado = @"
                SELECT TOP 1 IdEstado
                FROM Catalogos.EstadosCita
                WHERE Nombre = @NombreEstado;";

            int? idEstado = await db.QuerySingleOrDefaultAsync<int?>(
                sqlEstado,
                new { NombreEstado = nombreEstado.Trim() });

            if (!idEstado.HasValue)
            {
                throw new InvalidOperationException(
                    $"No se encontró el estado de cita '{nombreEstado}' en el catálogo.");
            }

            const string sqlActualizar = @"
                UPDATE Agenda.Citas
                SET IdEstado = @IdEstado
                WHERE IdCita = @IdCita;";

            int filas = await db.ExecuteAsync(sqlActualizar, new
            {
                IdCita = idCita,
                IdEstado = idEstado.Value
            });

            if (filas == 0)
            {
                throw new InvalidOperationException(
                    "La cita ya no existe o no pudo ser actualizada.");
            }
        }

        private static async Task<bool> TieneDuracionMinutosAsync(IDbConnection db)
        {
            const string sql = @"
                SELECT CASE
                    WHEN COL_LENGTH('Agenda.Citas', 'DuracionMinutos') IS NULL THEN 0
                    ELSE 1
                END;";

            int resultado = await db.ExecuteScalarAsync<int>(sql);
            return resultado == 1;
        }
    }
}
