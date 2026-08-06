using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.Repositories
{
    public class CitaRepository
    {
        private readonly string _connectionString;

        public CitaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Usamos dynamic porque la vista devuelve nombres cruzados (Paciente, Doctor, Estado) 
        // en lugar de solo IDs, ideal para mostrar en el DataGrid.
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

        public async Task AgendarAsync(CitaModel cita)
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

        public async Task CancelarAsync(int idCita)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { IdCita = idCita };
            // Cambia el estado internamente a 'Cancelada'
            await db.ExecuteAsync("Agenda.sp_CancelarCita", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
