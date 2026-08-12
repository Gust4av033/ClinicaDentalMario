using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class ConfiguracionViewModel : ViewModelBase
    {
        private readonly CatalogoRepository _catalogoRepo;

        // LISTA DE TRATAMIENTOS
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

        // CAMPOS PARA NUEVO TRATAMIENTO
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

        // COMANDOS
        public ICommand GuardarTratamientoCommand { get; }
        public ICommand EliminarTratamientoCommand { get; }

        public ConfiguracionViewModel()
        {
            Titulo = "Configuración del Sistema";
            _catalogoRepo = new CatalogoRepository();

            GuardarTratamientoCommand = new RelayCommand(async p => await GuardarTratamientoAsync());
            EliminarTratamientoCommand = new RelayCommand(async p => await EliminarTratamientoAsync(), p => TratamientoSeleccionado != null);

            _ = CargarTratamientosAsync();
        }

        private async Task CargarTratamientosAsync()
        {
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
            finally { EstaCargando = false; }
        }

        private async Task GuardarTratamientoAsync()
        {
            if (string.IsNullOrWhiteSpace(NuevoNombreTratamiento) || !decimal.TryParse(NuevoPrecioBase, out decimal precio))
            {
                MessageBox.Show("Por favor ingresa un nombre válido y un precio numérico.", "Datos inválidos", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("Tratamiento agregado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpiar campos y recargar tabla
                NuevoNombreTratamiento = string.Empty;
                NuevoPrecioBase = string.Empty;
                await CargarTratamientosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EliminarTratamientoAsync()
        {
            if (TratamientoSeleccionado == null) return;

            var result = MessageBox.Show($"¿Estás seguro de que deseas eliminar '{TratamientoSeleccionado.Nombre}' del catálogo?\n\n(No se borrará de los historiales pasados)", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
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
}