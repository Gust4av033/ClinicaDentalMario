using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using System.Data.SqlClient;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.Repositories
{
    public class PacienteRepository
    {
        private readonly string _connectionString;

        public PacienteRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<PacienteModel>> ObtenerTodosAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync<PacienteModel>("Pacientes.sp_ListarPacientes", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PacienteModel>> BuscarAsync(string termino)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { Termino = termino };
            return await db.QueryAsync<PacienteModel>("Pacientes.sp_BuscarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertarAsync(PacienteModel paciente)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
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

            // Retorna el Id generado por SCOPE_IDENTITY() desde el SP
            return await db.ExecuteScalarAsync<int>("Pacientes.sp_InsertarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarAsync(PacienteModel paciente)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
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
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { IdPaciente = idPaciente };
            // Realiza el soft delete (Activo = 0)
            await db.ExecuteAsync("Pacientes.sp_EliminarPaciente", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
