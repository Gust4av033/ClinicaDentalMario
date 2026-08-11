using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ClinicaDentalMario.Data;

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
