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
            bool baseExistente;

            // Primero solo consultamos master. Si la BD ya existe, nunca se recrea ni se
            // modifica automáticamente: puede contener información real de producción.
            using (var master = new SqlConnection(AppSettings.MasterConnectionString))
            {
                await master.OpenAsync();

                int existeBase = await master.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.databases WHERE name = @NombreBase",
                    new { NombreBase = AppSettings.DatabaseName });

                baseExistente = existeBase > 0;

                if (!baseExistente)
                {
                    // Instalación nueva: Script00 es el instalador inicial de la BD.
                    // Se ejecuta desde master para que pueda crear la base sin intentar
                    // eliminar una BD a la que la propia conexión esté conectada.
                    await EjecutarScriptInicialAsync(master);
                }
            }

            using var conn = new SqlConnection(AppSettings.ConnectionString);
            await conn.OpenAsync();

            if (!baseExistente)
            {
                // Solo una instalación nueva puede recibir ajustes automáticos de esquema.
                // Una BD existente nunca pasa por este método.
                await AplicarActualizacionesInstalacionNuevaAsync(conn);
            }

            // En producción esta operación es exclusivamente de lectura.
            await ValidarEstructuraMinimaAsync(conn);
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

        private static async Task ValidarEstructuraMinimaAsync(SqlConnection conn)
        {
            const string sql = @"
                SELECT
                    CASE WHEN OBJECT_ID('Pacientes.Pacientes', 'U') IS NOT NULL THEN 1 ELSE 0 END AS TienePacientes,
                    CASE WHEN OBJECT_ID('Pacientes.HistorialClinico', 'U') IS NOT NULL THEN 1 ELSE 0 END AS TieneHistorial,
                    CASE WHEN OBJECT_ID('Pacientes.AntecedentesPaciente', 'U') IS NOT NULL THEN 1 ELSE 0 END AS TieneAntecedentes,
                    CASE WHEN OBJECT_ID('Personal.Doctores', 'U') IS NOT NULL THEN 1 ELSE 0 END AS TieneDoctores,
                    CASE WHEN OBJECT_ID('Seguridad.Usuarios', 'U') IS NOT NULL THEN 1 ELSE 0 END AS TieneUsuarios;";

            var estado = await conn.QuerySingleAsync<EstructuraMinima>(sql);
            var faltantes = new List<string>();

            if (estado.TienePacientes == 0)
                faltantes.Add("Pacientes.Pacientes");
            if (estado.TieneHistorial == 0)
                faltantes.Add("Pacientes.HistorialClinico");
            if (estado.TieneAntecedentes == 0)
                faltantes.Add("Pacientes.AntecedentesPaciente");
            if (estado.TieneDoctores == 0)
                faltantes.Add("Personal.Doctores");
            if (estado.TieneUsuarios == 0)
                faltantes.Add("Seguridad.Usuarios");

            if (faltantes.Count > 0)
            {
                throw new InvalidOperationException(
                    "La base de datos existente no tiene toda la estructura requerida por esta versión. " +
                    "No se realizaron cambios automáticos. Faltan: " + string.Join(", ", faltantes));
            }
        }

        private static async Task AplicarActualizacionesInstalacionNuevaAsync(SqlConnection conn)
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

            const string sqlIndiceDuiUnico = @"
                IF OBJECT_ID('Pacientes.Pacientes', 'U') IS NOT NULL
                   AND NOT EXISTS (
                       SELECT 1
                       FROM sys.indexes
                       WHERE object_id = OBJECT_ID('Pacientes.Pacientes')
                         AND name = 'UX_Pacientes_DUI')
                   AND NOT EXISTS (
                       SELECT DUI
                       FROM Pacientes.Pacientes
                       WHERE DUI IS NOT NULL
                       GROUP BY DUI
                       HAVING COUNT(*) > 1)
                BEGIN
                    CREATE UNIQUE INDEX UX_Pacientes_DUI
                    ON Pacientes.Pacientes(DUI)
                    WHERE DUI IS NOT NULL;
                END;";

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
            await conn.ExecuteAsync(sqlIndiceDuiUnico);
            await conn.ExecuteAsync(sqlAgregarDuracionCita);
            await conn.ExecuteAsync(sqlNormalizarDuracionCita);
            await conn.ExecuteAsync(sqlConstraintDuracionCita);
            await conn.ExecuteAsync(sqlFuncionProximaCita);
        }

        private sealed class EstructuraMinima
        {
            public int TienePacientes { get; set; }
            public int TieneHistorial { get; set; }
            public int TieneAntecedentes { get; set; }
            public int TieneDoctores { get; set; }
            public int TieneUsuarios { get; set; }
        }
    }
}
