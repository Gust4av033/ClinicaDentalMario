using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class OdontogramaRepository
    {
        // Guarda el estado de los 32 dientes al mismo tiempo (Nueva Evolución)
        public async Task GuardarOdontogramaAsync(IEnumerable<OdontogramaModel> piezas)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            // 🔥 CAMBIO CLAVE: Quitamos GETDATE() y usamos @FechaRegistro 🔥
            string sql = @"
                INSERT INTO Odontologia.Odontograma 
                (IdPaciente, NumeroPieza, IdEstadoDental, Observaciones, FechaRegistro)
                VALUES 
                (@IdPaciente, @NumeroPieza, @IdEstadoDental, @Observaciones, @FechaRegistro)";

            await db.ExecuteAsync(sql, piezas);
        }

        // 🔥 NUEVO: Lista todas las fechas exactas donde se ha guardado un odontograma
        public async Task<IEnumerable<DateTime>> ListarFechasEvolucionesAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = "SELECT DISTINCT FechaRegistro FROM Odontologia.Odontograma WHERE IdPaciente = @IdPaciente ORDER BY FechaRegistro DESC";
            return await db.QueryAsync<DateTime>(sql, new { IdPaciente = idPaciente });
        }

        // 🔥 NUEVO: Carga los 32 dientes de una fecha y hora específica
        public async Task<IEnumerable<OdontogramaModel>> ObtenerOdontogramaPorFechaAsync(int idPaciente, DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = "SELECT * FROM Odontologia.Odontograma WHERE IdPaciente = @IdPaciente AND FechaRegistro = @Fecha";
            return await db.QueryAsync<OdontogramaModel>(sql, new { IdPaciente = idPaciente, Fecha = fecha });
        }

        // 🔥 NUEVO: Borra toda una evolución clínica (por si el doctor se equivocó)
        public async Task EliminarOdontogramaAsync(int idPaciente, DateTime fecha)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = "DELETE FROM Odontologia.Odontograma WHERE IdPaciente = @IdPaciente AND FechaRegistro = @Fecha";
            await db.ExecuteAsync(sql, new { IdPaciente = idPaciente, Fecha = fecha });
        }
    }
}