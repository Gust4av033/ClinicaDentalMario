using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class OdontogramaRepository
    {
        /// <summary>
        /// Guarda una evolución completa del odontograma en una sola transacción.
        /// Si una pieza falla, no queda una evolución parcial en la base de datos.
        /// </summary>
        public async Task GuardarOdontogramaAsync(IEnumerable<OdontogramaModel> piezas)
        {
            List<OdontogramaModel> registros = piezas?.ToList() ?? new List<OdontogramaModel>();
            if (registros.Count == 0)
            {
                return;
            }

            using IDbConnection db = DatabaseConnection.GetConnection();
            db.Open();
            using IDbTransaction transaction = db.BeginTransaction();

            const string sql = @"
                INSERT INTO Odontologia.Odontograma
                (IdPaciente, NumeroPieza, IdEstadoDental, Observaciones, FechaRegistro)
                VALUES
                (@IdPaciente, @NumeroPieza, @IdEstadoDental, @Observaciones, @FechaRegistro)";

            try
            {
                await db.ExecuteAsync(sql, registros, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// La tabla exige IdEstadoDental. El odontograma digital conserva el detalle clínico
        /// en Observaciones, por lo que se usa el estado base existente sin asumir que su Id es 1.
        /// No crea ni modifica catálogos.
        /// </summary>
        public async Task<int> ObtenerIdEstadoBaseAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT TOP 1 IdEstadoDental
                FROM Catalogos.CatalogoEstadosDentales
                WHERE Activo = 1
                ORDER BY CASE WHEN Nombre = 'Sano' THEN 0 ELSE 1 END, IdEstadoDental";

            int? id = await db.QueryFirstOrDefaultAsync<int?>(sql);
            if (!id.HasValue || id.Value <= 0)
            {
                throw new InvalidOperationException(
                    "No existe un estado dental activo en el catálogo requerido por el odontograma.");
            }

            return id.Value;
        }

        public async Task<IEnumerable<DateTime>> ListarFechasEvolucionesAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT DISTINCT FechaRegistro
                FROM Odontologia.Odontograma
                WHERE IdPaciente = @IdPaciente
                ORDER BY FechaRegistro DESC";

            return await db.QueryAsync<DateTime>(sql, new { IdPaciente = idPaciente });
        }

        public async Task<IEnumerable<OdontogramaModel>> ObtenerOdontogramaPorFechaAsync(
            int idPaciente,
            DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT IdRegistro, IdPaciente, NumeroPieza, IdEstadoDental, Observaciones, FechaRegistro
                FROM Odontologia.Odontograma
                WHERE IdPaciente = @IdPaciente
                  AND FechaRegistro = @Fecha
                ORDER BY NumeroPieza";

            return await db.QueryAsync<OdontogramaModel>(sql, new
            {
                IdPaciente = idPaciente,
                Fecha = fecha
            });
        }

        public async Task EliminarOdontogramaAsync(int idPaciente, DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                DELETE FROM Odontologia.Odontograma
                WHERE IdPaciente = @IdPaciente
                  AND FechaRegistro = @Fecha";

            await db.ExecuteAsync(sql, new
            {
                IdPaciente = idPaciente,
                Fecha = fecha
            });
        }
    }
}
