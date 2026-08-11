using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Data;

namespace ClinicaDentalMario.Repositories
{
    public class OdontogramaRepository
    {
        public async Task<IEnumerable<dynamic>> ListarOdontogramaAsync(int idPaciente)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdPaciente = idPaciente };
            // Usamos dynamic porque la vista devuelve nombres cruzados (Paciente, ColorHex, Estado)
            return await db.QueryAsync("Odontologia.sp_ListarOdontograma", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task InsertarEstadoDentalAsync(OdontogramaModel odontograma)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                odontograma.IdPaciente,
                odontograma.NumeroPieza,
                odontograma.IdEstadoDental,
                odontograma.Observaciones
            };
            await db.ExecuteAsync("Odontologia.sp_InsertarEstadoDental", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarPiezaDentalAsync(int idRegistro, int idEstadoDental, string observaciones)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                IdRegistro = idRegistro,
                IdEstadoDental = idEstadoDental,
                Observaciones = observaciones
            };
            await db.ExecuteAsync("Odontologia.sp_ActualizarPiezaDental", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
