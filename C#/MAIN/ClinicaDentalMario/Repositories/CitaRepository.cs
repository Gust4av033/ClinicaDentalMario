using ClinicaDentalMario.Config;
using ClinicaDentalMario.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Repositories
{
    public class CitaRepository
    {
        private readonly string _connectionString;

        // Constructor por defecto usando tu configuración global
        public CitaRepository()
        {
            _connectionString = AppSettings.ConnectionString;
        }

        // Método vital para la Agenda (El calendario llama a este método)
        public async Task<IEnumerable<dynamic>> ObtenerCitasPorFechaAsync(DateTime fecha)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = @"
                SELECT c.IdCita, c.IdPaciente, c.IdDoctor, c.FechaHora, c.Observaciones, 
                       p.NombreCompleto AS Paciente, d.NombreCompleto AS Doctor, e.Nombre AS Estado
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON c.IdPaciente = p.IdPaciente
                INNER JOIN Personal.Doctores d ON c.IdDoctor = d.IdDoctor
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE CAST(c.FechaHora AS DATE) = CAST(@Fecha AS DATE) 
                  AND e.Nombre != 'Cancelada'
                ORDER BY c.FechaHora ASC";

            return await db.QueryAsync<dynamic>(sql, new { Fecha = fecha });
        }

        public async Task<IEnumerable<dynamic>> ObtenerAgendaHoyAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync("Agenda.sp_ListarAgendaDia", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<dynamic>> ObtenerAgendaSemanaAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync("Agenda.sp_ListarAgendaSemana", commandType: CommandType.StoredProcedure);
        }

        public async Task InsertarAsync(CitaModel cita)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new
            {
                cita.IdPaciente,
                cita.IdDoctor,
                cita.IdEstado,
                cita.FechaHora,
                cita.Observaciones
            };

            await db.ExecuteAsync("Agenda.sp_AgendarCita", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarCitaAsync(int idCita, DateTime fechaHora, string observaciones)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "UPDATE Agenda.Citas SET FechaHora = @FechaHora, Observaciones = @Observaciones WHERE IdCita = @IdCita";
            await db.ExecuteAsync(sql, new { IdCita = idCita, FechaHora = fechaHora, Observaciones = observaciones });
        }

        public async Task CancelarCitaAsync(int idCita)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { IdCita = idCita };
            await db.ExecuteAsync("Agenda.sp_CancelarCita", parameters, commandType: CommandType.StoredProcedure);
        }

        // 🔥 NUEVO MÉTODO PARA FINALIZAR CITAS (O cambiar a cualquier estado) 🔥
        public async Task CambiarEstadoCitaAsync(int idCita, string nombreEstado)
        {
            using IDbConnection db = new SqlConnection(_connectionString);

            // Usamos una subconsulta para encontrar el IdEstado correcto basado en la palabra ("Finalizada")
            string sql = @"
                UPDATE Agenda.Citas 
                SET IdEstado = (SELECT IdEstado FROM Catalogos.EstadosCita WHERE Nombre = @NombreEstado) 
                WHERE IdCita = @IdCita";

            await db.ExecuteAsync(sql, new { IdCita = idCita, NombreEstado = nombreEstado });
        }
    }
}