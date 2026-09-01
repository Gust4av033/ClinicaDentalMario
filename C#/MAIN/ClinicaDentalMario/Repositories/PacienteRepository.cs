using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class PacienteRepository
    {
        public async Task<IEnumerable<PacienteModel>> ObtenerTodosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            return await db.QueryAsync<PacienteModel>(
                "Pacientes.sp_ListarPacientes",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PacienteModel>> BuscarAsync(string termino, bool soloInactivos = false)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT
                    IdPaciente,
                    NombreCompleto,
                    Direccion,
                    FechaNacimiento,
                    Sexo,
                    DUI,
                    Telefono,
                    NombreEncargado,
                    ContactoEmergencia,
                    TelefonoEmergencia,
                    FechaRegistro,
                    Activo
                FROM Pacientes.Pacientes
                WHERE Activo = @Activo
                  AND (
                      NombreCompleto LIKE '%' + @Termino + '%'
                      OR DUI LIKE '%' + @Termino + '%'
                      OR Telefono LIKE '%' + @Termino + '%'
                  )
                ORDER BY NombreCompleto;";

            return await db.QueryAsync<PacienteModel>(
                sql,
                new
                {
                    Termino = termino?.Trim() ?? string.Empty,
                    Activo = soloInactivos ? 0 : 1
                });
        }

        public async Task<bool> ExisteDuiEnOtroPacienteAsync(string dui, int? excluirIdPaciente = null)
        {
            if (string.IsNullOrWhiteSpace(dui))
            {
                return false;
            }

            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT COUNT(1)
                FROM Pacientes.Pacientes
                WHERE DUI = @DUI
                  AND (@ExcluirIdPaciente IS NULL OR IdPaciente <> @ExcluirIdPaciente);";

            int cantidad = await db.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    DUI = dui.Trim(),
                    ExcluirIdPaciente = excluirIdPaciente
                });

            return cantidad > 0;
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

            return await db.ExecuteScalarAsync<int>(
                "Pacientes.sp_InsertarPaciente",
                parameters,
                commandType: CommandType.StoredProcedure);
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

            await db.ExecuteAsync(
                "Pacientes.sp_EditarPaciente",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task EliminarAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            await db.ExecuteAsync(
                "Pacientes.sp_EliminarPaciente",
                new { IdPaciente = idPaciente },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<PacienteModel>> ObtenerInactivosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT
                    IdPaciente,
                    NombreCompleto,
                    Direccion,
                    FechaNacimiento,
                    Sexo,
                    DUI,
                    Telefono,
                    NombreEncargado,
                    ContactoEmergencia,
                    TelefonoEmergencia,
                    FechaRegistro,
                    Activo
                FROM Pacientes.Pacientes
                WHERE Activo = 0
                ORDER BY NombreCompleto;";

            return await db.QueryAsync<PacienteModel>(sql);
        }

        public async Task RestaurarAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                UPDATE Pacientes.Pacientes
                SET Activo = 1
                WHERE IdPaciente = @IdPaciente;";

            await db.ExecuteAsync(sql, new { IdPaciente = idPaciente });
        }
    }
}
