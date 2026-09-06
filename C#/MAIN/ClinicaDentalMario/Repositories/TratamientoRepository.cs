using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class TratamientoRepository
    {
        private static readonly HashSet<string> EstadosPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pendiente",
            "En progreso",
            "Finalizado",
            "Cancelado"
        };

        public async Task<IEnumerable<TratamientoPacienteModel>> ObtenerPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT
                    tp.Id,
                    tp.IdPaciente,
                    tp.IdDoctor,
                    tp.IdTratamiento,
                    p.NombreCompleto AS NombrePaciente,
                    d.NombreCompleto AS NombreDoctor,
                    d.Especialidad AS EspecialidadDoctor,
                    ct.Nombre AS NombreTratamiento,
                    ct.Descripcion AS DescripcionTratamiento,
                    tp.CostoTotal,
                    tp.Estado,
                    tp.FechaInicio,
                    tp.FechaFin,
                    tp.Observaciones
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Pacientes.Pacientes p
                    ON tp.IdPaciente = p.IdPaciente
                INNER JOIN Personal.Doctores d
                    ON tp.IdDoctor = d.IdDoctor
                INNER JOIN Catalogos.CatalogoTratamientos ct
                    ON tp.IdTratamiento = ct.IdTratamiento
                WHERE tp.IdPaciente = @IdPaciente
                ORDER BY
                    CASE tp.Estado
                        WHEN 'En progreso' THEN 0
                        WHEN 'Pendiente' THEN 1
                        WHEN 'Finalizado' THEN 2
                        WHEN 'Cancelado' THEN 3
                        ELSE 4
                    END,
                    tp.FechaInicio DESC,
                    tp.Id DESC;";

            return await db.QueryAsync<TratamientoPacienteModel>(
                sql,
                new { IdPaciente = idPaciente });
        }

        public async Task CrearTratamientoAsync(TratamientoPacienteModel tratamiento)
        {
            ArgumentNullException.ThrowIfNull(tratamiento);

            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                tratamiento.IdPaciente,
                tratamiento.IdDoctor,
                tratamiento.IdTratamiento,
                tratamiento.CostoTotal,
                Observaciones = NormalizarTexto(tratamiento.Observaciones)
            };

            await db.ExecuteAsync(
                "Odontologia.sp_CrearTratamiento",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task FinalizarTratamientoAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                UPDATE Odontologia.TratamientosPaciente
                SET Estado = 'Finalizado',
                    FechaFin = GETDATE()
                WHERE Id = @IdTratamientoPaciente
                  AND Estado = 'En progreso';";

            int filas = await db.ExecuteAsync(sql, new
            {
                IdTratamientoPaciente = idTratamientoPaciente
            });

            if (filas == 0)
            {
                throw new InvalidOperationException(
                    "Solo un tratamiento en progreso puede marcarse como finalizado.");
            }
        }

        public async Task CambiarEstadoTratamientoAsync(
            int idTratamientoPaciente,
            string nuevoEstado)
        {
            if (string.IsNullOrWhiteSpace(nuevoEstado))
                throw new ArgumentException("Debe indicar un estado válido.", nameof(nuevoEstado));

            string estadoNormalizado = NormalizarEstado(nuevoEstado);

            if (!EstadosPermitidos.Contains(estadoNormalizado))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nuevoEstado),
                    $"El estado '{nuevoEstado}' no es válido para un tratamiento.");
            }

            if (estadoNormalizado.Equals("Finalizado", StringComparison.OrdinalIgnoreCase))
            {
                await FinalizarTratamientoAsync(idTratamientoPaciente);
                return;
            }

            using IDbConnection db = DatabaseConnection.GetConnection();

            string sql = estadoNormalizado switch
            {
                "En progreso" => @"
                    UPDATE Odontologia.TratamientosPaciente
                    SET Estado = 'En progreso',
                        FechaInicio = GETDATE(),
                        FechaFin = NULL
                    WHERE Id = @IdTratamientoPaciente
                      AND Estado = 'Pendiente';",
                "Cancelado" => @"
                    UPDATE Odontologia.TratamientosPaciente
                    SET Estado = 'Cancelado',
                        FechaFin = GETDATE()
                    WHERE Id = @IdTratamientoPaciente
                      AND Estado IN ('Pendiente', 'En progreso');",
                "Pendiente" => @"
                    UPDATE Odontologia.TratamientosPaciente
                    SET Estado = 'Pendiente'
                    WHERE Id = @IdTratamientoPaciente
                      AND Estado = 'Pendiente';",
                _ => throw new InvalidOperationException("Transición de estado no soportada.")
            };

            int filas = await db.ExecuteAsync(sql, new
            {
                IdTratamientoPaciente = idTratamientoPaciente
            });

            if (filas == 0)
            {
                throw new InvalidOperationException(
                    "El tratamiento cambió de estado o la transición solicitada ya no es válida.");
            }
        }

        public async Task<int?> ObtenerIdTratamientoActivoAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT TOP 1 Id
                FROM Odontologia.TratamientosPaciente
                WHERE IdPaciente = @IdPaciente
                  AND Estado = 'En progreso'
                ORDER BY FechaInicio DESC, Id DESC;";

            return await db.QueryFirstOrDefaultAsync<int?>(
                sql,
                new { IdPaciente = idPaciente });
        }

        public async Task ActualizarTratamientoAsync(
            int idTratamientoPaciente,
            decimal costoTotal,
            string? observaciones)
        {
            if (costoTotal < 0)
                throw new ArgumentOutOfRangeException(nameof(costoTotal), "El costo no puede ser negativo.");

            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                UPDATE Odontologia.TratamientosPaciente
                SET CostoTotal = @CostoTotal,
                    Observaciones = @Observaciones
                WHERE Id = @IdTratamientoPaciente
                  AND Estado NOT IN ('Finalizado', 'Cancelado');";

            int filas = await db.ExecuteAsync(sql, new
            {
                CostoTotal = costoTotal,
                Observaciones = NormalizarTexto(observaciones),
                IdTratamientoPaciente = idTratamientoPaciente
            });

            if (filas == 0)
            {
                throw new InvalidOperationException(
                    "El tratamiento ya no existe o su estado actual no permite editarlo.");
            }
        }

        public async Task<IEnumerable<dynamic>> ObtenerProductividadAsync(
            DateTime fechaInicio,
            DateTime fechaFin)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            const string sql = @"
                SELECT
                    t.Nombre AS Tratamiento,
                    COUNT(tp.Id) AS Cantidad,
                    SUM(tp.CostoTotal) AS IngresoProyectado
                FROM Odontologia.TratamientosPaciente tp
                INNER JOIN Catalogos.CatalogoTratamientos t
                    ON tp.IdTratamiento = t.IdTratamiento
                WHERE tp.FechaInicio >= @Inicio
                  AND tp.FechaInicio < @Fin
                  AND tp.Estado <> 'Cancelado'
                GROUP BY t.Nombre
                ORDER BY Cantidad DESC;";

            return await db.QueryAsync<dynamic>(sql, new
            {
                Inicio = fechaInicio,
                Fin = fechaFin.Date.AddDays(1)
            });
        }

        private static string? NormalizarTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        private static string NormalizarEstado(string estado)
        {
            if (estado.Equals("en progreso", StringComparison.OrdinalIgnoreCase))
                return "En progreso";
            if (estado.Equals("pendiente", StringComparison.OrdinalIgnoreCase))
                return "Pendiente";
            if (estado.Equals("finalizado", StringComparison.OrdinalIgnoreCase))
                return "Finalizado";
            if (estado.Equals("cancelado", StringComparison.OrdinalIgnoreCase))
                return "Cancelado";

            return estado.Trim();
        }
    }
}
