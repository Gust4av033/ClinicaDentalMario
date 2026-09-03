using ClinicaDentalMario.Config;
using Dapper;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace ClinicaDentalMario.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InicializarBaseDeDatosAsync()
        {
            bool baseCreadaAhora;

            using (var conn = new SqlConnection(AppSettings.MasterConnectionString))
            {
                await conn.OpenAsync();

                int existeBase = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @NombreBase",
                    new { NombreBase = AppSettings.DatabaseName });

                baseCreadaAhora = existeBase == 0;

                if (baseCreadaAhora)
                {
                    await conn.ExecuteAsync($"CREATE DATABASE [{AppSettings.DatabaseName}];");
                }
            }

            using (var conn = new SqlConnection(AppSettings.ConnectionString))
            {
                await conn.OpenAsync();

                int existeEstructura = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Pacientes' AND t.name = 'Pacientes'");

                if (existeEstructura == 0)
                {
                    if (!baseCreadaAhora)
                    {
                        throw new InvalidOperationException(
                            "La base de datos existe, pero no contiene la estructura esperada. No se ejecutará el script inicial automáticamente para evitar pérdida de datos.");
                    }

                    await EjecutarScriptInicialAsync(conn);
                }

                await AplicarActualizacionesEsquemaAsync(conn);
            }
        }

        private static async Task EjecutarScriptInicialAsync(SqlConnection conn)
        {
            string rutaScript = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Scripts",
                "Script00.sql");

            if (!File.Exists(rutaScript))
            {
                throw new FileNotFoundException(
                    "No se encontró el script inicial de la base de datos.",
                    rutaScript);
            }

            string contenidoScript = await File.ReadAllTextAsync(rutaScript);
            var comandos = Regex.Split(
                contenidoScript,
                @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var comando in comandos)
            {
                if (string.IsNullOrWhiteSpace(comando))
                {
                    continue;
                }

                try
                {
                    await conn.ExecuteAsync(comando);
                }
                catch (SqlException ex)
                {
                    if (ex.Number != 2714 && ex.Number != 2627)
                    {
                        throw;
                    }
                }
            }
        }

        private static async Task AplicarActualizacionesEsquemaAsync(SqlConnection conn)
        {
            const string sqlAntecedentesPaciente = @"
                IF OBJECT_ID('Pacientes.AntecedentesPaciente', 'U') IS NULL
                BEGIN
                    CREATE TABLE Pacientes.AntecedentesPaciente
                    (
                        IdPaciente INT NOT NULL PRIMARY KEY,
                        TieneAntecedentesMedicos BIT NOT NULL CONSTRAINT DF_AntecedentesPaciente_TieneMedicos DEFAULT 0,
                        DetalleAntecedentesMedicos NVARCHAR(MAX) NULL,
                        TieneAntecedentesOdontologicos BIT NOT NULL CONSTRAINT DF_AntecedentesPaciente_TieneOdontologicos DEFAULT 0,
                        DetalleAntecedentesOdontologicos NVARCHAR(MAX) NULL,
                        FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_AntecedentesPaciente_FechaRegistro DEFAULT SYSDATETIME(),
                        FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_AntecedentesPaciente_FechaActualizacion DEFAULT SYSDATETIME(),
                        CONSTRAINT FK_AntecedentesPaciente_Paciente
                            FOREIGN KEY (IdPaciente)
                            REFERENCES Pacientes.Pacientes(IdPaciente)
                    );
                END;";

            // IMPORTANTE: agregar la columna y crear el CHECK deben ejecutarse en batches
            // separados. SQL Server compila el batch completo antes de ejecutar el ALTER,
            // por lo que referenciar una columna recién agregada en el mismo batch puede
            // producir "Invalid column name 'DuracionMinutos'".
            const string sqlAgregarDuracionCita = @"
                IF OBJECT_ID('Agenda.Citas', 'U') IS NOT NULL
                   AND COL_LENGTH('Agenda.Citas', 'DuracionMinutos') IS NULL
                BEGIN
                    ALTER TABLE Agenda.Citas
                    ADD DuracionMinutos INT NOT NULL
                        CONSTRAINT DF_Citas_DuracionMinutos DEFAULT 30 WITH VALUES;
                END;";

            const string sqlNormalizarDuracionCita = @"
                IF OBJECT_ID('Agenda.Citas', 'U') IS NOT NULL
                   AND COL_LENGTH('Agenda.Citas', 'DuracionMinutos') IS NOT NULL
                BEGIN
                    UPDATE Agenda.Citas
                    SET DuracionMinutos = 30
                    WHERE DuracionMinutos IS NULL
                       OR DuracionMinutos NOT IN (15, 30, 45, 60, 90);
                END;";

            const string sqlConstraintDuracionCita = @"
                IF OBJECT_ID('Agenda.Citas', 'U') IS NOT NULL
                   AND COL_LENGTH('Agenda.Citas', 'DuracionMinutos') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.check_constraints
                       WHERE name = 'CK_Citas_DuracionMinutos'
                         AND parent_object_id = OBJECT_ID('Agenda.Citas'))
                BEGIN
                    ALTER TABLE Agenda.Citas WITH CHECK
                    ADD CONSTRAINT CK_Citas_DuracionMinutos
                    CHECK (DuracionMinutos IN (15, 30, 45, 60, 90));
                END;";

            const string sqlFuncionProximaCita = @"
                CREATE OR ALTER FUNCTION Agenda.fnProximaCita (@IdPaciente INT)
                RETURNS DATETIME
                AS
                BEGIN
                    DECLARE @Proxima DATETIME;

                    SELECT TOP 1 @Proxima = c.FechaHora
                    FROM Agenda.Citas c
                    WHERE c.IdPaciente = @IdPaciente
                      AND c.FechaHora >= GETDATE()
                      AND c.IdEstado IN (
                          SELECT IdEstado
                          FROM Catalogos.EstadosCita
                          WHERE Nombre IN ('Confirmada', 'Pendiente'))
                    ORDER BY c.FechaHora ASC;

                    RETURN @Proxima;
                END;";

            await conn.ExecuteAsync(sqlAntecedentesPaciente);
            await conn.ExecuteAsync(sqlAgregarDuracionCita);
            await conn.ExecuteAsync(sqlNormalizarDuracionCita);
            await conn.ExecuteAsync(sqlConstraintDuracionCita);
            await conn.ExecuteAsync(sqlFuncionProximaCita);
        }
    }
}
