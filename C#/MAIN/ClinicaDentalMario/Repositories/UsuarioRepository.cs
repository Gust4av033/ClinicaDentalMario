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

            return await db.QueryFirstOrDefaultAsync<UsuarioModel>(
                sql,
                new
                {
                    Usuario = usuario,
                    PasswordHash = passwordHash
                },
                commandTimeout: 10);
        }

        public async Task<IEnumerable<UsuarioModel>> ListarUsuariosAsync()
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
                    u.FechaCreacion,
                    r.Nombre AS NombreRol
                FROM Seguridad.Usuarios u
                INNER JOIN Seguridad.Roles r ON u.IdRol = r.IdRol
                ORDER BY u.Activo DESC, u.NombreCompleto ASC;";

            return await db.QueryAsync<UsuarioModel>(sql);
        }

        public async Task<IEnumerable<RolModel>> ListarRolesAsync()
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT IdRol, Nombre, Descripcion, Activo
                FROM Seguridad.Roles
                WHERE Activo = 1
                ORDER BY Nombre;";

            return await db.QueryAsync<RolModel>(sql);
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, int? excluirId = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT COUNT(1)
                FROM Seguridad.Usuarios
                WHERE LOWER(LTRIM(RTRIM(Usuario))) = LOWER(LTRIM(RTRIM(@Usuario)))
                  AND (@ExcluirId IS NULL OR IdUsuario <> @ExcluirId);";

            int cantidad = await db.ExecuteScalarAsync<int>(sql, new
            {
                Usuario = nombreUsuario,
                ExcluirId = excluirId
            });

            return cantidad > 0;
        }

        public async Task<int> ContarAdministradoresActivosAsync(int? excluirId = null)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                SELECT COUNT(1)
                FROM Seguridad.Usuarios u
                INNER JOIN Seguridad.Roles r ON u.IdRol = r.IdRol
                WHERE u.Activo = 1
                  AND r.Nombre = 'Administrador'
                  AND (@ExcluirId IS NULL OR u.IdUsuario <> @ExcluirId);";

            return await db.ExecuteScalarAsync<int>(sql, new { ExcluirId = excluirId });
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

            await db.ExecuteAsync(
                "Seguridad.sp_CrearUsuario",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task ActualizarUsuarioAsync(UsuarioModel usuario)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            const string sql = @"
                UPDATE Seguridad.Usuarios
                SET IdRol = @IdRol,
                    NombreCompleto = @NombreCompleto,
                    Usuario = @NombreUsuario,
                    Correo = @Correo,
                    Activo = @Activo
                WHERE IdUsuario = @IdUsuario;";

            await db.ExecuteAsync(sql, new
            {
                usuario.IdRol,
                usuario.NombreCompleto,
                usuario.NombreUsuario,
                usuario.Correo,
                usuario.Activo,
                usuario.IdUsuario
            });
        }

        public async Task CambiarPasswordAsync(int idUsuario, string nuevoPasswordHash)
        {
            using IDbConnection db = DatabaseConnection.GetConnection();

            await db.ExecuteAsync(
                "Seguridad.sp_CambiarPassword",
                new
                {
                    IdUsuario = idUsuario,
                    NuevoPasswordHash = nuevoPasswordHash
                },
                commandType: CommandType.StoredProcedure);
        }
    }
}
