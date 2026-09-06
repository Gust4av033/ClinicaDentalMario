using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class DoctorRepository
    {
        public async Task<IEnumerable<DoctorModel>> ObtenerDoctoresActivosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                SELECT IdDoctor, NombreCompleto, Especialidad, Telefono, Correo, Direccion, NumeroJVPO, CAST(1 AS BIT) AS Activo
                FROM Personal.vwDoctores
                ORDER BY NombreCompleto ASC;";

            return await db.QueryAsync<DoctorModel>(query);
        }

        public async Task<IEnumerable<DoctorModel>> ObtenerDoctoresAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                SELECT IdDoctor, NombreCompleto, Especialidad, Telefono, Correo, Direccion, NumeroJVPO, Activo
                FROM Personal.Doctores
                ORDER BY Activo DESC, NombreCompleto ASC;";

            return await db.QueryAsync<DoctorModel>(query);
        }

        public async Task<bool> ExisteNumeroJVPOAsync(string numeroJVPO, int? excluirId = null)
        {
            if (string.IsNullOrWhiteSpace(numeroJVPO))
                return false;

            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                SELECT COUNT(1)
                FROM Personal.Doctores
                WHERE NumeroJVPO IS NOT NULL
                  AND LOWER(LTRIM(RTRIM(NumeroJVPO))) = LOWER(LTRIM(RTRIM(@NumeroJVPO)))
                  AND (@ExcluirId IS NULL OR IdDoctor <> @ExcluirId);";

            int cantidad = await db.ExecuteScalarAsync<int>(query, new
            {
                NumeroJVPO = numeroJVPO,
                ExcluirId = excluirId
            });

            return cantidad > 0;
        }

        public async Task<int> ContarCitasFuturasAsync(int idDoctor)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                SELECT COUNT(1)
                FROM Agenda.Citas c
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE c.IdDoctor = @IdDoctor
                  AND c.FechaHora >= GETDATE()
                  AND e.Nombre NOT IN ('Cancelada', 'Atendida', 'No Asistió');";

            return await db.ExecuteScalarAsync<int>(query, new { IdDoctor = idDoctor });
        }

        public async Task CrearDoctorAsync(DoctorModel doctor)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                INSERT INTO Personal.Doctores
                    (NombreCompleto, Especialidad, Telefono, Correo, Direccion, NumeroJVPO, Activo)
                VALUES
                    (@NombreCompleto, @Especialidad, @Telefono, @Correo, @Direccion, @NumeroJVPO, 1);";

            await db.ExecuteAsync(query, doctor);
        }

        public async Task ActualizarDoctorAsync(DoctorModel doctor)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                UPDATE Personal.Doctores
                SET NombreCompleto = @NombreCompleto,
                    Especialidad = @Especialidad,
                    Telefono = @Telefono,
                    Correo = @Correo,
                    Direccion = @Direccion,
                    NumeroJVPO = @NumeroJVPO
                WHERE IdDoctor = @IdDoctor;";

            await db.ExecuteAsync(query, doctor);
        }

        public async Task CambiarEstadoDoctorAsync(int idDoctor, bool activo)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                UPDATE Personal.Doctores
                SET Activo = @Activo
                WHERE IdDoctor = @IdDoctor;";

            await db.ExecuteAsync(query, new
            {
                IdDoctor = idDoctor,
                Activo = activo
            });
        }
    }
}
