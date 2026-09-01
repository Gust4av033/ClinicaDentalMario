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

            const string sql = @"
                SELECT IdPaciente, NombreCompleto, Direccion, FechaNacimiento, Sexo, DUI, Telefono,
                       NombreEncargado, ContactoEmergencia, TelefonoEmergencia, FechaRegistro, Activo
                FROM Pacientes.Pacientes
                WHERE Activo = 1
                ORDER BY NombreCompleto;";

            return await db.QueryAsync<PacienteModel>(sql);
        }

        public async Task<IEnumerable<PacienteModel>> BuscarAsync(string termino, bool soloInactivos = false)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT IdPaciente, NombreCompleto, Direccion, FechaNacimiento, Sexo, DUI, Telefono,
                       NombreEncargado, ContactoEmergencia, TelefonoEmergencia, FechaRegistro, Activo
                FROM Pacientes.Pacientes
                WHERE Activo = @Activo
                  AND (NombreCompleto LIKE '%' + @Termino + '%'
                       OR DUI LIKE '%' + @Termino + '%'
                       OR Telefono LIKE '%' + @Termino + '%')
                ORDER BY NombreCompleto;";

            return await db.QueryAsync<PacienteModel>(sql, new
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

            int cantidad = await db.ExecuteScalarAsync<int>(sql, new
            {
                DUI = dui.Trim(),
                ExcluirIdPaciente = excluirIdPaciente
            });

            return cantidad > 0;
        }

        public async Task<int> InsertarConAntecedentesAsync(
            PacienteModel paciente,
            AntecedentesPacienteModel antecedentes)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            db.Open();
            using IDbTransaction transaction = db.BeginTransaction();

            try
            {
                var parameters = CrearParametrosPaciente(paciente);

                int idPaciente = await db.ExecuteScalarAsync<int>(
                    "Pacientes.sp_InsertarPaciente",
                    parameters,
                    transaction,
                    commandType: CommandType.StoredProcedure);

                antecedentes.IdPaciente = idPaciente;
                await GuardarAntecedentesAsync(db, antecedentes, transaction);
                transaction.Commit();
                return idPaciente;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<AntecedentesPacienteModel?> ObtenerAntecedentesAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT IdPaciente, TieneAntecedentesMedicos, DetalleAntecedentesMedicos,
                       TieneAntecedentesOdontologicos, DetalleAntecedentesOdontologicos,
                       FechaRegistro, FechaActualizacion
                FROM Pacientes.AntecedentesPaciente
                WHERE IdPaciente = @IdPaciente;";

            return await db.QuerySingleOrDefaultAsync<AntecedentesPacienteModel>(
                sql,
                new { IdPaciente = idPaciente });
        }

        public async Task GuardarAntecedentesAsync(AntecedentesPacienteModel antecedentes)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            await GuardarAntecedentesAsync(db, antecedentes, null);
        }

        public async Task ActualizarConAntecedentesAsync(
            PacienteModel paciente,
            AntecedentesPacienteModel antecedentes)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            db.Open();
            using IDbTransaction transaction = db.BeginTransaction();

            try
            {
                await db.ExecuteAsync(
                    "Pacientes.sp_EditarPaciente",
                    CrearParametrosPaciente(paciente, incluirIdPaciente: true),
                    transaction,
                    commandType: CommandType.StoredProcedure);

                antecedentes.IdPaciente = paciente.IdPaciente;
                await GuardarAntecedentesAsync(db, antecedentes, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static async Task GuardarAntecedentesAsync(
            IDbConnection db,
            AntecedentesPacienteModel antecedentes,
            IDbTransaction? transaction)
        {
            const string sql = @"
                UPDATE Pacientes.AntecedentesPaciente
                SET TieneAntecedentesMedicos = @TieneAntecedentesMedicos,
                    DetalleAntecedentesMedicos = @DetalleAntecedentesMedicos,
                    TieneAntecedentesOdontologicos = @TieneAntecedentesOdontologicos,
                    DetalleAntecedentesOdontologicos = @DetalleAntecedentesOdontologicos,
                    FechaActualizacion = SYSDATETIME()
                WHERE IdPaciente = @IdPaciente;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO Pacientes.AntecedentesPaciente
                    (IdPaciente, TieneAntecedentesMedicos, DetalleAntecedentesMedicos,
                     TieneAntecedentesOdontologicos, DetalleAntecedentesOdontologicos)
                    VALUES
                    (@IdPaciente, @TieneAntecedentesMedicos, @DetalleAntecedentesMedicos,
                     @TieneAntecedentesOdontologicos, @DetalleAntecedentesOdontologicos);
                END;";

            await db.ExecuteAsync(sql, antecedentes, transaction);
        }

        public async Task ActualizarAsync(PacienteModel paciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            await db.ExecuteAsync(
                "Pacientes.sp_EditarPaciente",
                CrearParametrosPaciente(paciente, incluirIdPaciente: true),
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
                SELECT IdPaciente, NombreCompleto, Direccion, FechaNacimiento, Sexo, DUI, Telefono,
                       NombreEncargado, ContactoEmergencia, TelefonoEmergencia, FechaRegistro, Activo
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

        private static object CrearParametrosPaciente(PacienteModel paciente, bool incluirIdPaciente = false)
        {
            return incluirIdPaciente
                ? new
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
                }
                : new
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
        }
    }
}
