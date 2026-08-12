using ClinicaDentalMario.Data;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class ReporteRepository
    {
        public async Task<IEnumerable<dynamic>> ObtenerIngresosDiariosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            //[cite_start]// Consume la vista Facturacion.vwIngresosDiarios [cite: 703, 798]
            string query = "SELECT * FROM Facturacion.vwIngresosDiarios ORDER BY Fecha DESC";
            return await db.QueryAsync(query);
        }

        public async Task<IEnumerable<dynamic>> ObtenerIngresosMensualesAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // [cite_start]// Consume la vista Facturacion.vwIngresosMensuales [cite: 704, 800]
            string query = "SELECT * FROM Facturacion.vwIngresosMensuales ORDER BY Anio DESC, Mes DESC";
            return await db.QueryAsync(query);
        }

        public async Task<IEnumerable<dynamic>> ObtenerSaldosMorososAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            //  [cite_start]// Consume la vista Pacientes.vwSaldoPacientes [cite: 706, 803] y filtra los que deben
            string query = "SELECT * FROM Pacientes.vwSaldoPacientes WHERE (TotalCargos - TotalPagado) > 0";
            return await db.QueryAsync(query);
        }
    }
}
