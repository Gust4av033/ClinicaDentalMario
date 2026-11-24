using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public sealed class DashboardRepository
    {
        public async Task<DashboardResumenModel> ObtenerResumenAsync(DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            DateTime inicioDia = fecha.Date;
            DateTime finDia = inicioDia.AddDays(1);
            DateTime inicioMes = new(inicioDia.Year, inicioDia.Month, 1);
            DateTime finMes = inicioMes.AddMonths(1);

            const string sql = @"
                SELECT
                    (SELECT ISNULL(SUM(pg.Monto), 0)
                     FROM Odontologia.Pagos pg
                     WHERE pg.FechaPago >= @InicioDia AND pg.FechaPago < @FinDia) AS IngresosHoy,

                    (SELECT ISNULL(SUM(pg.Monto), 0)
                     FROM Odontologia.Pagos pg
                     WHERE pg.FechaPago >= @InicioMes AND pg.FechaPago < @FinMes) AS IngresosMes,

                    (SELECT COUNT(*)
                     FROM Pacientes.Pacientes p
                     WHERE p.Activo = 1) AS PacientesActivos,

                    (SELECT COUNT(*)
                     FROM Pacientes.Pacientes p
                     WHERE p.Activo = 1
                       AND p.FechaRegistro >= @InicioMes AND p.FechaRegistro < @FinMes) AS PacientesNuevosMes,

                    (SELECT COUNT(*)
                     FROM Agenda.Citas c
                     WHERE c.FechaHora >= @InicioDia AND c.FechaHora < @FinDia) AS CitasHoy,

                    (SELECT COUNT(*)
                     FROM Agenda.Citas c
                     INNER JOIN Catalogos.EstadosCita e ON e.IdEstado = c.IdEstado
                     WHERE c.FechaHora >= @InicioDia AND c.FechaHora < @FinDia
                       AND e.Nombre = 'Pendiente') AS CitasPendientes,

                    (SELECT COUNT(*)
                     FROM Agenda.Citas c
                     INNER JOIN Catalogos.EstadosCita e ON e.IdEstado = c.IdEstado
                     WHERE c.FechaHora >= @InicioDia AND c.FechaHora < @FinDia
                       AND e.Nombre = 'Confirmada') AS CitasConfirmadas,

                    (SELECT COUNT(*)
                     FROM Agenda.Citas c
                     INNER JOIN Catalogos.EstadosCita e ON e.IdEstado = c.IdEstado
                     WHERE c.FechaHora >= @InicioDia AND c.FechaHora < @FinDia
                       AND e.Nombre = 'Atendida') AS CitasAtendidas,

                    (SELECT COUNT(*)
                     FROM Odontologia.TratamientosPaciente tp
                     WHERE tp.Estado IN ('Pendiente', 'En progreso')) AS TratamientosActivos,

                    (SELECT COUNT(*)
                     FROM Odontologia.TratamientosPaciente tp
                     WHERE tp.Estado = 'Pendiente') AS TratamientosPendientes,

                    (SELECT COUNT(*)
                     FROM Odontologia.TratamientosPaciente tp
                     WHERE tp.Estado = 'En progreso') AS TratamientosEnProgreso,

                    (SELECT ISNULL(SUM(
                        CASE
                            WHEN tp.CostoTotal > ISNULL(pagos.TotalAbonado, 0)
                                THEN tp.CostoTotal - ISNULL(pagos.TotalAbonado, 0)
                            ELSE 0
                        END), 0)
                     FROM Odontologia.TratamientosPaciente tp
                     LEFT JOIN (
                         SELECT IdTratamientoPaciente, SUM(Monto) AS TotalAbonado
                         FROM Odontologia.Pagos
                         GROUP BY IdTratamientoPaciente
                     ) pagos ON pagos.IdTratamientoPaciente = tp.Id
                     WHERE tp.Estado IN ('Pendiente', 'En progreso')) AS SaldoPendiente;";

            return await db.QuerySingleAsync<DashboardResumenModel>(sql, new
            {
                InicioDia = inicioDia,
                FinDia = finDia,
                InicioMes = inicioMes,
                FinMes = finMes
            });
        }

        public async Task<IReadOnlyList<DashboardCitaModel>> ObtenerCitasDelDiaAsync(
            DateTime fecha,
            int limite = 12)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            DateTime inicioDia = fecha.Date;
            DateTime finDia = inicioDia.AddDays(1);

            const string sql = @"
                SELECT TOP (@Limite)
                    c.IdCita,
                    c.FechaHora,
                    p.NombreCompleto AS Paciente,
                    d.NombreCompleto AS Doctor,
                    e.Nombre AS Estado,
                    c.Observaciones
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON p.IdPaciente = c.IdPaciente
                INNER JOIN Personal.Doctores d ON d.IdDoctor = c.IdDoctor
                INNER JOIN Catalogos.EstadosCita e ON e.IdEstado = c.IdEstado
                WHERE c.FechaHora >= @InicioDia AND c.FechaHora < @FinDia
                ORDER BY c.FechaHora ASC;";

            var resultado = await db.QueryAsync<DashboardCitaModel>(sql, new
            {
                InicioDia = inicioDia,
                FinDia = finDia,
                Limite = limite
            });

            return resultado.AsList();
        }

        public async Task<DashboardCitaModel?> ObtenerProximaCitaAsync(DateTime desde)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT TOP 1
                    c.IdCita,
                    c.FechaHora,
                    p.NombreCompleto AS Paciente,
                    d.NombreCompleto AS Doctor,
                    e.Nombre AS Estado,
                    c.Observaciones
                FROM Agenda.Citas c
                INNER JOIN Pacientes.Pacientes p ON p.IdPaciente = c.IdPaciente
                INNER JOIN Personal.Doctores d ON d.IdDoctor = c.IdDoctor
                INNER JOIN Catalogos.EstadosCita e ON e.IdEstado = c.IdEstado
                WHERE c.FechaHora >= @Desde
                  AND e.Nombre NOT IN ('Cancelada', 'No Asistió', 'Atendida')
                ORDER BY c.FechaHora ASC;";

            return await db.QueryFirstOrDefaultAsync<DashboardCitaModel>(sql, new { Desde = desde });
        }

        public async Task<IReadOnlyList<DashboardSaldoPacienteModel>> ObtenerSaldosPendientesAsync(
            int limite = 5)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT TOP (@Limite)
                    p.IdPaciente,
                    p.NombreCompleto AS Paciente,
                    p.Telefono,
                    CAST(SUM(
                        CASE
                            WHEN tp.CostoTotal > ISNULL(pagos.TotalAbonado, 0)
                                THEN tp.CostoTotal - ISNULL(pagos.TotalAbonado, 0)
                            ELSE 0
                        END
                    ) AS DECIMAL(10, 2)) AS SaldoPendiente
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Pacientes.Pacientes p ON p.IdPaciente = tp.IdPaciente
                LEFT JOIN (
                    SELECT IdTratamientoPaciente, SUM(Monto) AS TotalAbonado
                    FROM Odontologia.Pagos
                    GROUP BY IdTratamientoPaciente
                ) pagos ON pagos.IdTratamientoPaciente = tp.Id
                WHERE tp.Estado IN ('Pendiente', 'En progreso')
                GROUP BY p.IdPaciente, p.NombreCompleto, p.Telefono
                HAVING SUM(
                    CASE
                        WHEN tp.CostoTotal > ISNULL(pagos.TotalAbonado, 0)
                            THEN tp.CostoTotal - ISNULL(pagos.TotalAbonado, 0)
                        ELSE 0
                    END
                ) > 0
                ORDER BY SaldoPendiente DESC, p.NombreCompleto ASC;";

            var resultado = await db.QueryAsync<DashboardSaldoPacienteModel>(sql, new { Limite = limite });
            return resultado.AsList();
        }
    }
}
