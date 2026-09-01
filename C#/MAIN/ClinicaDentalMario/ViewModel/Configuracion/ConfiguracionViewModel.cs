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
    public class ConfiguracionViewModel : ViewModelBase
    {
        private readonly CatalogoRepository _catalogoRepo;
        private readonly IPermissionService _permissionService;

        private ObservableCollection<CatalogoTratamientosModel> _listaTratamientos = new();
        public ObservableCollection<CatalogoTratamientosModel> ListaTratamientos
        {
            get => _listaTratamientos;
            set => SetProperty(ref _listaTratamientos, value);
        }

        private CatalogoTratamientosModel? _tratamientoSeleccionado;
        public CatalogoTratamientosModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set => SetProperty(ref _tratamientoSeleccionado, value);
        }

        private string _nuevoNombreTratamiento = string.Empty;
        public string NuevoNombreTratamiento
        {
            get => _nuevoNombreTratamiento;
            set => SetProperty(ref _nuevoNombreTratamiento, value);
        }

        private string _nuevoPrecioBase = string.Empty;
        public string NuevoPrecioBase
        {
            get => _nuevoPrecioBase;
            set => SetProperty(ref _nuevoPrecioBase, value);
        }

        public ICommand GuardarTratamientoCommand { get; }
        public ICommand EliminarTratamientoCommand { get; }

        public ConfiguracionViewModel()
        {
            Titulo = "Configuración del Sistema";
            _catalogoRepo = new CatalogoRepository();
            _permissionService = new PermissionService();

            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                throw new UnauthorizedAccessException("El usuario actual no puede acceder a Configuración.");
            }

            GuardarTratamientoCommand = new RelayCommand(async p => await GuardarTratamientoAsync());
            EliminarTratamientoCommand = new RelayCommand(
                async p => await EliminarTratamientoAsync(),
                p => TratamientoSeleccionado != null);

            _ = CargarTratamientosAsync();
        }

        private async Task CargarTratamientosAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                return;
            }

            EstaCargando = true;
            try
            {
                var tratamientos = await _catalogoRepo.ObtenerTratamientosActivosAsync();
                ListaTratamientos = new ObservableCollection<CatalogoTratamientosModel>(tratamientos);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el catálogo: " + ex.Message);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task GuardarTratamientoAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                MessageBox.Show(
                    "ACCESO DENEGADO: Solo los Administradores pueden modificar los catálogos de precios.",
                    "Seguridad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NuevoNombreTratamiento) ||
                !decimal.TryParse(NuevoPrecioBase, out decimal precio))
            {
                MessageBox.Show(
                    "Por favor ingresa un nombre válido y un precio numérico.",
                    "Datos inválidos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var nuevoTratamiento = new CatalogoTratamientosModel
                {
                    Nombre = NuevoNombreTratamiento.Trim(),
                    Descripcion = "Agregado por usuario",
                    PrecioBase = precio
                };

                await _catalogoRepo.InsertarTratamientoAsync(nuevoTratamiento);
                MessageBox.Show(
                    "Tratamiento agregado con éxito.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                NuevoNombreTratamiento = string.Empty;
                NuevoPrecioBase = string.Empty;
                await CargarTratamientosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al guardar: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task EliminarTratamientoAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                MessageBox.Show(
                    "ACCESO DENEGADO: No tienes permisos para eliminar tratamientos.",
                    "Seguridad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (TratamientoSeleccionado == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"¿Estás seguro de que deseas eliminar '{TratamientoSeleccionado.Nombre}'?\n\n(No se borrará de los historiales pasados)",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await _catalogoRepo.EliminarTratamientoAsync(TratamientoSeleccionado.IdTratamiento);
                await CargarTratamientosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message);
            }
        }
    }
}
