using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    /// <summary>
    /// Consultas de solo lectura para reportes. No crea ni modifica objetos de base de datos.
    /// </summary>
    public class ReporteRepository
    {
        public async Task<IEnumerable<dynamic>> ObtenerIngresosDiariosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string query = "SELECT * FROM Facturacion.vwIngresosDiarios ORDER BY Fecha DESC";
            return await db.QueryAsync(query);
        }

        public async Task<IEnumerable<dynamic>> ObtenerIngresosMensualesAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string query = "SELECT * FROM Facturacion.vwIngresosMensuales ORDER BY Anio DESC, Mes DESC";
            return await db.QueryAsync(query);
        }

        public async Task<IEnumerable<dynamic>> ObtenerSaldosMorososAsync()
        {
            // Compatibilidad con el método antiguo, pero aplicando la misma regla actual:
            // únicamente Pendiente/En progreso generan saldo cobrable.
            var saldos = await ObtenerSaldosPacientesAsync();
            return saldos.Cast<dynamic>();
        }

        public async Task<IEnumerable<ReporteSaldoPacienteModel>> ObtenerSaldosPacientesAsync(string? termino = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
SELECT
    p.IdPaciente,
    p.NombreCompleto,
    p.Telefono,
    CAST(ISNULL(cargos.TotalCargos, 0) AS DECIMAL(10,2)) AS TotalCargos,
    CAST(ISNULL(pagos.TotalPagado, 0) AS DECIMAL(10,2)) AS TotalPagado,
    CAST(ISNULL(cargos.TotalCargos, 0) - ISNULL(pagos.TotalPagado, 0) AS DECIMAL(10,2)) AS SaldoPendiente
FROM Pacientes.Pacientes p
OUTER APPLY
(
    SELECT SUM(tp.CostoTotal) AS TotalCargos
    FROM Odontologia.TratamientosPaciente tp
    WHERE tp.IdPaciente = p.IdPaciente
      AND tp.Estado IN ('Pendiente', 'En progreso')
) cargos
OUTER APPLY
(
    SELECT SUM(pg.Monto) AS TotalPagado
    FROM Odontologia.Pagos pg
    INNER JOIN Odontologia.TratamientosPaciente tp
        ON tp.Id = pg.IdTratamientoPaciente
    WHERE tp.IdPaciente = p.IdPaciente
      AND tp.Estado IN ('Pendiente', 'En progreso')
) pagos
WHERE p.Activo = 1
  AND ISNULL(cargos.TotalCargos, 0) - ISNULL(pagos.TotalPagado, 0) > 0
  AND (
        @Termino IS NULL
        OR p.NombreCompleto LIKE '%' + @Termino + '%'
        OR p.DUI LIKE '%' + @Termino + '%'
        OR p.Telefono LIKE '%' + @Termino + '%'
      )
ORDER BY SaldoPendiente DESC, p.NombreCompleto ASC;";

            string? terminoNormalizado = string.IsNullOrWhiteSpace(termino)
                ? null
                : termino.Trim();

            return await db.QueryAsync<ReporteSaldoPacienteModel>(sql, new
            {
                Termino = terminoNormalizado
            });
        }
    }
}
