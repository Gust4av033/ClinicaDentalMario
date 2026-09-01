using ClinicaDentalMario.Common;

namespace ClinicaDentalMario.Services
{
    public interface IPermissionService
    {
        bool TienePermiso(PermisoSistema permiso);
    }
}
