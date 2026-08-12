using ClinicaDentalMario.Data;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class BitacoraRepository
    {
        public async Task<IEnumerable<dynamic>> ListarMovimientosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // Trae los movimientos más recientes primero
            string query = "SELECT * FROM Seguridad.Bitacora ORDER BY Fecha DESC";
            return await db.QueryAsync(query);
        }
    }
}
