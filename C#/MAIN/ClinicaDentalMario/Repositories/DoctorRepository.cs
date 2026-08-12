using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class DoctorRepository
    {
        public async Task<IEnumerable<DoctorModel>> ObtenerDoctoresActivosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            // Leemos directamente la vista para llenar los desplegables rápidamente
            string query = "SELECT * FROM Personal.vwDoctores";
            return await db.QueryAsync<DoctorModel>(query);
        }

        public async Task CrearDoctorAsync(DoctorModel doctor)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string query = @"INSERT INTO Personal.Doctores 
                            (NombreCompleto, Especialidad, Telefono, Correo, Direccion, NumeroJVPO) 
                            VALUES (@NombreCompleto, @Especialidad, @Telefono, @Correo, @Direccion, @NumeroJVPO)";
            await db.ExecuteAsync(query, doctor);
        }
    }
}
