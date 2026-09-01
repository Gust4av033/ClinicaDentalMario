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
            using (var conn = new SqlConnection(AppSettings.MasterConnectionString))
            {
                string crearBaseDeDatos = $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{AppSettings.DatabaseName}') CREATE DATABASE [{AppSettings.DatabaseName}];";
                await conn.ExecuteAsync(crearBaseDeDatos);
            }

            using (var conn = new SqlConnection(AppSettings.ConnectionString))
            {
                await conn.OpenAsync();

                int existeEstructura = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Pacientes' AND t.name = 'Pacientes'");

                if (existeEstructura == 0)
                {
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
                return;
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

            const string sqlDuracionCita = @"
                IF OBJECT_ID('Agenda.Citas', 'U') IS NOT NULL
                   AND COL_LENGTH('Agenda.Citas', 'DuracionMinutos') IS NULL
                BEGIN
                    ALTER TABLE Agenda.Citas
                    ADD DuracionMinutos INT NOT NULL
                        CONSTRAINT DF_Citas_DuracionMinutos DEFAULT 30 WITH VALUES;
                END;";

            await conn.ExecuteAsync(sqlAntecedentesPaciente);
            await conn.ExecuteAsync(sqlDuracionCita);
        }
    }
}
