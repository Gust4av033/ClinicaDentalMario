using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class BitacoraViewModel : ViewModelBase
    {
        private readonly IPermissionService _permissionService;
        private readonly BitacoraRepository _bitacoraRepository;

        private ObservableCollection<BitacoraModel> _registrosAuditoria = new();
        public ObservableCollection<BitacoraModel> RegistrosAuditoria
        {
            get => _registrosAuditoria;
            set => SetProperty(ref _registrosAuditoria, value);
        }

        private string _textoBusqueda = string.Empty;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set => SetProperty(ref _textoBusqueda, value);
        }

        public ICommand CargarBitacoraCommand { get; }
        public ICommand BuscarCommand { get; }

        public BitacoraViewModel()
        {
            Titulo = "Auditoría y Bitácora del Sistema";
            _permissionService = new PermissionService();
            _bitacoraRepository = new BitacoraRepository();

            if (!_permissionService.TienePermiso(PermisoSistema.VerBitacora))
            {
                throw new UnauthorizedAccessException("El usuario actual no puede consultar la bitácora.");
            }

            CargarBitacoraCommand = new RelayCommand(async _ => await CargarAsync());
            BuscarCommand = new RelayCommand(async _ => await CargarAsync());

            _ = CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.VerBitacora))
            {
                MessageBox.Show(
                    "Solo los Administradores pueden ver la Bitácora del sistema.",
                    "Acceso Denegado");
                return;
            }

            EstaCargando = true;
            try
            {
                var result = await _bitacoraRepository.ListarMovimientosAsync(TextoBusqueda);
                RegistrosAuditoria = new ObservableCollection<BitacoraModel>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar la bitácora: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}
