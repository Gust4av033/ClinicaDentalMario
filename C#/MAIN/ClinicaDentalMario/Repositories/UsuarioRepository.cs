using ClinicaDentalMario.Data;
using ClinicaDentalMario.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ClinicaDentalMario.Repositories
{
    public class UsuarioRepository
    {
        public async Task<UsuarioModel?> LoginAsync(string usuario, string passwordHash)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT
                    u.IdUsuario,
                    u.IdRol,
                    u.NombreCompleto,
                    u.Usuario AS NombreUsuario,
                    u.Correo,
                    u.Activo,
                    r.Nombre AS NombreRol
                FROM Seguridad.Usuarios u
                INNER JOIN Seguridad.Roles r ON u.IdRol = r.IdRol
                WHERE u.Usuario = @Usuario
                  AND u.PasswordHash = @PasswordHash
                  AND u.Activo = 1;";

            var parameters = new
            {
                Usuario = usuario,
                PasswordHash = passwordHash
            };

            return await db.QueryFirstOrDefaultAsync<UsuarioModel>(
                sql,
                parameters,
                commandTimeout: 10);
        }

        public async Task<IEnumerable<UsuarioModel>> ListarUsuariosAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                SELECT u.IdUsuario, u.IdRol, u.NombreCompleto, u.Usuario AS NombreUsuario, u.Correo, u.Activo, u.FechaCreacion, r.Nombre AS NombreRol
                FROM Seguridad.Usuarios u
                INNER JOIN Seguridad.Roles r ON u.IdRol = r.IdRol
                ORDER BY u.NombreCompleto ASC";

            return await db.QueryAsync<UsuarioModel>(sql);
        }

        public async Task<IEnumerable<RolModel>> ListarRolesAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = "SELECT IdRol, Nombre, Descripcion, Activo FROM Seguridad.Roles WHERE Activo = 1";
            return await db.QueryAsync<RolModel>(sql);
        }

        public async Task CrearUsuarioAsync(UsuarioModel nuevoUsuario)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new
            {
                nuevoUsuario.IdRol,
                nuevoUsuario.NombreCompleto,
                Usuario = nuevoUsuario.NombreUsuario,
                nuevoUsuario.Correo,
                nuevoUsuario.PasswordHash
            };
            await db.ExecuteAsync("Seguridad.sp_CrearUsuario", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarUsuarioAsync(UsuarioModel usuario)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            string sql = @"
                UPDATE Seguridad.Usuarios
                SET IdRol = @IdRol, NombreCompleto = @NombreCompleto, Correo = @Correo, Activo = @Activo
                WHERE IdUsuario = @IdUsuario";

            await db.ExecuteAsync(sql, new
            {
                usuario.IdRol,
                usuario.NombreCompleto,
                usuario.Correo,
                usuario.Activo,
                usuario.IdUsuario
            });
        }

        public async Task CambiarPasswordAsync(int idUsuario, string nuevoPasswordHash)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();
            var parameters = new { IdUsuario = idUsuario, NuevoPasswordHash = nuevoPasswordHash };
            await db.ExecuteAsync("Seguridad.sp_CambiarPassword", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
