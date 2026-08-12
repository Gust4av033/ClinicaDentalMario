using ClinicaDentalMario.Data;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class DashboardRepository
    {
        public async Task<decimal> ObtenerIngresosDelDiaAsync(DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            // Suma los montos de los pagos realizados en la fecha indicada
            string sql = "SELECT ISNULL(SUM(Monto), 0) FROM Odontologia.Pagos WHERE CAST(FechaPago AS DATE) = CAST(@Fecha AS DATE)";

            return await db.ExecuteScalarAsync<decimal>(sql, new { Fecha = fecha });
        }

        public async Task<int> ObtenerTotalCitasHoyAsync(DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            // CORRECCIÓN: La tabla está en Agenda.Citas, no en Odontologia
            string sql = "SELECT COUNT(*) FROM Agenda.Citas WHERE CAST(FechaHora AS DATE) = CAST(@Fecha AS DATE)";

            return await db.ExecuteScalarAsync<int>(sql, new { Fecha = fecha });
        }

        // 🟢 1. OBTENER LAS CITAS DE HOY (Para la tabla)
        public async Task<IEnumerable<dynamic>> ObtenerCitasHoyListaAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT 
                    FORMAT(c.FechaHora, 'hh:mm tt') AS Hora,
                    p.NombreCompleto AS Paciente,
                    ISNULL(c.Observaciones, 'Consulta General') AS Tratamiento,
                    e.Nombre AS Estado
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON c.IdPaciente = p.IdPaciente
                INNER JOIN Catalogos.EstadosCita e ON c.IdEstado = e.IdEstado
                WHERE CAST(c.FechaHora AS DATE) = CAST(GETDATE() AS DATE)
                ORDER BY c.FechaHora ASC";

            return await db.QueryAsync<dynamic>(sql);
        }

        // 🔴 2. OBTENER LOS PACIENTES MOROSOS (Saldo Pendiente > 0)
        public async Task<IEnumerable<dynamic>> ObtenerMorososAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT 
                    p.NombreCompleto AS Paciente,
                    (tp.CostoTotal - ISNULL((SELECT SUM(Monto) FROM Odontologia.Pagos pg WHERE pg.IdTratamientoPaciente = tp.Id), 0)) AS Saldo
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Pacientes.Pacientes p ON tp.IdPaciente = p.IdPaciente
                WHERE tp.Estado = 'En progreso'
                AND (tp.CostoTotal - ISNULL((SELECT SUM(Monto) FROM Odontologia.Pagos pg WHERE pg.IdTratamientoPaciente = tp.Id), 0)) > 0
                ORDER BY Saldo DESC";

            return await db.QueryAsync<dynamic>(sql);
        }

        // 🟡 3. OBTENER CUMPLEAÑEROS DEL MES
        public async Task<IEnumerable<dynamic>> ObtenerCumpleanerosMesAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT 
                    NombreCompleto AS Paciente,
                    CONCAT(DAY(FechaNacimiento), ' de este mes') AS Fecha
                FROM Pacientes.Pacientes
                WHERE MONTH(FechaNacimiento) = MONTH(GETDATE()) 
                AND Activo = 1
                ORDER BY DAY(FechaNacimiento) ASC";

            return await db.QueryAsync<dynamic>(sql);
        }
    }
}