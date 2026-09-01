using ClinicaDentalMario.Common;

namespace ClinicaDentalMario.Services
{
    /// <summary>
    /// Centraliza las reglas de acceso por rol.
    /// Las reglas específicas se irán completando conforme se cierre cada módulo.
    /// </summary>
    public sealed class PermissionService : IPermissionService
    {
        public bool TienePermiso(PermisoSistema permiso)
        {
            if (!UsuarioActual.EstaAutenticado)
            {
                return false;
            }

            if (UsuarioActual.EsAdministrador)
            {
                return true;
            }

            if (!UsuarioActual.EsDoctor && !UsuarioActual.EsRecepcionista)
            {
                return false;
            }

            return permiso switch
            {
                PermisoSistema.AdministrarConfiguracion => false,
                PermisoSistema.AdministrarUsuarios => false,
                PermisoSistema.VerBitacora => false,
                _ => true
            };
        }
    }
}
