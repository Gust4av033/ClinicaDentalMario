using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.Repositories
{
    public class TratamientoRepository
    {
        private readonly string _connectionString;

        public TratamientoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<dynamic>> ObtenerPorPacienteAsync(int idPaciente)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { IdPaciente = idPaciente };
            return await db.QueryAsync("Odontologia.sp_ListarTratamientosPaciente", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task CrearTratamientoAsync(TratamientoPacienteModel tratamiento)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new
            {
                tratamiento.IdPaciente,
                tratamiento.IdDoctor,
                tratamiento.IdTratamiento,
                tratamiento.CostoTotal,
                tratamiento.Observaciones
            };

            await db.ExecuteAsync("Odontologia.sp_CrearTratamiento", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task FinalizarTratamientoAsync(int idTratamientoPaciente)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new { IdTratamientoPaciente = idTratamientoPaciente };
            await db.ExecuteAsync("Odontologia.sp_FinalizarTratamiento", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
