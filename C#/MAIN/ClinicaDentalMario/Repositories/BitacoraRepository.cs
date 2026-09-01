using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class BitacoraRepository
    {
        public async Task<IEnumerable<BitacoraModel>> ListarMovimientosAsync(string? textoBusqueda = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT
                    IdBitacora,
                    Usuario AS NombreUsuario,
                    Accion,
                    (Tabla + ' | ' + ISNULL(RegistroAfectado, '')) AS Detalles,
                    Fecha
                FROM Seguridad.Bitacora
                WHERE (@Texto = ''
                    OR Usuario LIKE '%' + @Texto + '%'
                    OR Accion LIKE '%' + @Texto + '%')
                ORDER BY Fecha DESC";

            return await db.QueryAsync<BitacoraModel>(sql, new
            {
                Texto = textoBusqueda?.Trim() ?? string.Empty
            });
        }
    }
}
