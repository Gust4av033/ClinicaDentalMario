using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.Common
{
    /// <summary>
    /// Representa la sesión actual de la aplicación.
    /// Mantiene los datos mínimos del usuario autenticado y evita repetir lógica de roles.
    /// </summary>
    public static class UsuarioActual
    {
        public static UsuarioModel? Detalles { get; private set; }

        public static string NombreRol { get; private set; } = string.Empty;

        public static bool EstaAutenticado => Detalles is not null;

        public static string NombreCompleto => Detalles?.NombreCompleto ?? string.Empty;

        public static string NombreUsuario => Detalles?.NombreUsuario ?? string.Empty;

        public static bool EsAdministrador => TieneRol(RolesSistema.Administrador);

        public static bool EsDoctor => TieneRol(RolesSistema.Doctor);

        public static bool EsRecepcionista => TieneRol(RolesSistema.Recepcionista);

        public static event EventHandler? SesionCambiada;

        public static void IniciarSesion(UsuarioModel usuario, string rol)
        {
            ArgumentNullException.ThrowIfNull(usuario);

            Detalles = usuario;
            NombreRol = rol?.Trim() ?? string.Empty;
            SesionCambiada?.Invoke(null, EventArgs.Empty);
        }

        public static void CerrarSesion()
        {
            Detalles = null;
            NombreRol = string.Empty;
            SesionCambiada?.Invoke(null, EventArgs.Empty);
        }

        public static bool TieneRol(string rol)
        {
            return EstaAutenticado &&
                   string.Equals(NombreRol, rol, StringComparison.OrdinalIgnoreCase);
        }
    }
}
