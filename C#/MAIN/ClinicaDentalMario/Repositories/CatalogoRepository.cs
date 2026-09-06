using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class CatalogoRepository
    {
        public async Task<IEnumerable<CatalogoTratamientosModel>> ObtenerTratamientosActivosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                SELECT IdTratamiento, Nombre, Descripcion, PrecioBase, DuracionMinutos
                FROM Catalogos.vwTratamientos
                ORDER BY Nombre ASC;";

            return await db.QueryAsync<CatalogoTratamientosModel>(query);
        }

        public async Task<bool> ExisteNombreTratamientoAsync(string nombre, int? excluirId = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                SELECT COUNT(1)
                FROM Catalogos.CatalogoTratamientos
                WHERE Activo = 1
                  AND LOWER(LTRIM(RTRIM(Nombre))) = LOWER(LTRIM(RTRIM(@Nombre)))
                  AND (@ExcluirId IS NULL OR IdTratamiento <> @ExcluirId);";

            int cantidad = await db.ExecuteScalarAsync<int>(query, new
            {
                Nombre = nombre,
                ExcluirId = excluirId
            });

            return cantidad > 0;
        }

        public async Task InsertarTratamientoAsync(CatalogoTratamientosModel tratamiento)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                INSERT INTO Catalogos.CatalogoTratamientos
                    (Nombre, Descripcion, PrecioBase, DuracionMinutos, Activo)
                VALUES
                    (@Nombre, @Descripcion, @PrecioBase, @DuracionMinutos, 1);";

            await db.ExecuteAsync(sql, tratamiento);
        }

        public async Task ActualizarTratamientoAsync(CatalogoTratamientosModel tratamiento)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                UPDATE Catalogos.CatalogoTratamientos
                SET Nombre = @Nombre,
                    Descripcion = @Descripcion,
                    PrecioBase = @PrecioBase,
                    DuracionMinutos = @DuracionMinutos
                WHERE IdTratamiento = @IdTratamiento
                  AND Activo = 1;";

            await db.ExecuteAsync(sql, tratamiento);
        }

        public async Task ActualizarPrecioTratamientoAsync(int idTratamiento, decimal nuevoPrecio)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string query = @"
                UPDATE Catalogos.CatalogoTratamientos
                SET PrecioBase = @NuevoPrecio
                WHERE IdTratamiento = @IdTratamiento
                  AND Activo = 1;";

            await db.ExecuteAsync(query, new
            {
                NuevoPrecio = nuevoPrecio,
                IdTratamiento = idTratamiento
            });
        }

        public async Task EliminarTratamientoAsync(int idTratamiento)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                UPDATE Catalogos.CatalogoTratamientos
                SET Activo = 0
                WHERE IdTratamiento = @IdTratamiento;";

            await db.ExecuteAsync(sql, new { IdTratamiento = idTratamiento });
        }
    }
}
