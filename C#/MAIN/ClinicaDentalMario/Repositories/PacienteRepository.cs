using ClinicaDentalMario.Config;
using ClinicaDentalMario.Data; // <-- Asegúrate de incluir este using para tu DatabaseConnection
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

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

        public async Task<IEnumerable<PacienteModel>> ObtenerInactivosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // Si ya creaste el SP, úsalo. Si no, esta consulta asume que haces un borrado lógico (Activo = 0)
            string sql = "SELECT * FROM Pacientes.Pacientes WHERE Activo = 0 ORDER BY NombreCompleto";
            return await db.QueryAsync<PacienteModel>(sql);
        }

        public async Task RestaurarAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // Ajusta el nombre de tu tabla y columna si es necesario
            string sql = "UPDATE Pacientes.Pacientes SET Activo = 1 WHERE IdPaciente = @IdPaciente";
            await db.ExecuteAsync(sql, new { IdPaciente = idPaciente });
        }
    }
}