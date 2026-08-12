using Dapper;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace ClinicaDentalMario.Data
{
    public static class DatabaseInitializer
    {
        private static string _masterConn = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;";
        private static string _dbConn = @"Server=(localdb)\MSSQLLocalDB;Database=ClinicaDentalMario;Integrated Security=true;TrustServerCertificate=True;Encrypt=False;";

        public static async Task InicializarBaseDeDatosAsync()
        {
            // 1. Crear la base de datos en master si no existe
            using (var conn = new SqlConnection(_masterConn))
            {
                await conn.ExecuteAsync("IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ClinicaDentalMario') CREATE DATABASE ClinicaDentalMario;");
            }

            // 2. Verificar si ya se inicializó el sistema previamente
            using (var conn = new SqlConnection(_dbConn))
            {
                await conn.OpenAsync();

                // Verificamos si la tabla principal de pacientes ya existe en la BD
                int existeEstructura = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'Pacientes' AND t.name = 'Pacientes'"
                );

                // Si la estructura YA existe, NO volvemos a correr el script para evitar duplicados en los catálogos
                if (existeEstructura > 0)
                {
                    return;
                }

                // 3. Si es la primera vez (instalación limpia), leemos y ejecutamos el Script00.sql
                string rutaScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "Script00.sql");

                if (File.Exists(rutaScript))
                {
                    string contenidoScript = await File.ReadAllTextAsync(rutaScript);

                    // Dividimos el script utilizando los bloques 'GO'
                    var comandos = Regex.Split(contenidoScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                    foreach (var comando in comandos)
                    {
                        if (!string.IsNullOrWhiteSpace(comando))
                        {
                            try
                            {
                                await conn.ExecuteAsync(comando);
                            }
                            catch (SqlException ex)
                            {
                                // Ignoramos únicamente si el objeto ya existe (código 2714) o llave duplicada (código 2627) por seguridad
                                if (ex.Number != 2714 && ex.Number != 2627)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}