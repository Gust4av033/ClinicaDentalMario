using ClinicaDentalMario.Config;
using ClinicaDentalMario.Data; // <-- Asegúrate de incluir este using para tu DatabaseConnection
using ClinicaDentalMario.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Repositories
{
    public class PacienteRepository
    {
        private readonly string _connectionString;

        // Constructor vacío que por defecto usa tu DatabaseConnection segura
        public PacienteRepository()
        {
            _connectionString = AppSettings.ConnectionString;
        }

        // Constructor opcional por si en algún momento necesitas pasarle una cadena personalizada
        public PacienteRepository(string connectionString)
        {
            _connectionString = string.IsNullOrEmpty(connectionString) ? AppSettings.ConnectionString : connectionString;
        }

        public async Task<IEnumerable<PacienteModel>> ObtenerTodosAsync()
        {
            // Usamos tu DatabaseConnection directamente o la variable interna
            using IDbConnection db = DatabaseConnection.GetConnection();
            return await db.QueryAsync<PacienteModel>("Pacientes.sp_ListarPacientes", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PacienteModel>> BuscarAsync(string termino)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { Termino = termino };
            return await db.QueryAsync<PacienteModel>("Pacientes.sp_BuscarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertarAsync(PacienteModel paciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                paciente.NombreCompleto,
                paciente.Direccion,
                paciente.FechaNacimiento,
                paciente.Sexo,
                paciente.DUI,
                paciente.Telefono,
                paciente.NombreEncargado,
                paciente.ContactoEmergencia,
                paciente.TelefonoEmergencia
            };

            return await db.ExecuteScalarAsync<int>("Pacientes.sp_InsertarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarAsync(PacienteModel paciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                paciente.IdPaciente,
                paciente.NombreCompleto,
                paciente.Direccion,
                paciente.FechaNacimiento,
                paciente.Sexo,
                paciente.DUI,
                paciente.Telefono,
                paciente.NombreEncargado,
                paciente.ContactoEmergencia,
                paciente.TelefonoEmergencia
            };

            await db.ExecuteAsync("Pacientes.sp_EditarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task EliminarAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };
            await db.ExecuteAsync("Pacientes.sp_EliminarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}