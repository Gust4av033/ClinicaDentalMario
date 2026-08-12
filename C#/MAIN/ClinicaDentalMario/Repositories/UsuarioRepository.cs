using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Data;

namespace ClinicaDentalMario.Repositories
{
    public class UsuarioRepository
    {
        public async Task<UsuarioModel?> LoginAsync(string usuario, string passwordHash)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { Usuario = usuario, PasswordHash = passwordHash };

            // [cite_start]// Llama al SP_Login que valida credenciales y si está activo [cite: 770, 819]
            // Usamos QueryFirstOrDefaultAsync porque esperamos un solo usuario o null si falla
            return await db.QueryFirstOrDefaultAsync<UsuarioModel>("Seguridad.sp_Login", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task CrearUsuarioAsync(UsuarioModel nuevoUsuario)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                nuevoUsuario.IdRol,
                nuevoUsuario.NombreCompleto,
                Usuario = nuevoUsuario.NombreUsuario, // Mapeo a como lo espera el SP
                nuevoUsuario.Correo,
                nuevoUsuario.PasswordHash
            };
            // [cite_start]// [cite: 769, 821, 822]
            await db.ExecuteAsync("Seguridad.sp_CrearUsuario", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task CambiarPasswordAsync(int idUsuario, string nuevoPasswordHash)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdUsuario = idUsuario, NuevoPasswordHash = nuevoPasswordHash };
            // [cite_start]// [cite: 767, 820]
            await db.ExecuteAsync("Seguridad.sp_CambiarPassword", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
