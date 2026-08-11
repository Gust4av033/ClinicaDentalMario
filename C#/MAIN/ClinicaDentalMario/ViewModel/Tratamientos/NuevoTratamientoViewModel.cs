using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Tratamientos;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class NuevoTratamientoViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly TratamientoRepository _tratamientoRepo;
        private readonly CatalogoRepository _catalogoRepo;

        private TratamientoPacienteModel _nuevoTratamiento;
        public TratamientoPacienteModel NuevoTratamiento
        {
            get => _nuevoTratamiento;
            set => SetProperty(ref _nuevoTratamiento, value);
        }

        private ObservableCollection<CatalogoTratamientosModel> _listaCatalogo = new();
        public ObservableCollection<CatalogoTratamientosModel> ListaCatalogo
        {
            get => _listaCatalogo;
            set => SetProperty(ref _listaCatalogo, value);
        }

        private CatalogoTratamientosModel? _tratamientoSeleccionado;
        public CatalogoTratamientosModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (SetProperty(ref _tratamientoSeleccionado, value) && value != null)
                {
                    // Al seleccionar un tratamiento del catálogo, sugerimos el precio base
                    NuevoTratamiento.CostoTotal = value.PrecioBase;

                    // Notificamos a la vista que el objeto NuevoTratamiento cambió
                    OnPropertyChanged(nameof(NuevoTratamiento));
                }
            }
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoTratamientoViewModel(int idPaciente, Action<object> cambiarVista)
        {
            Titulo = "Asignar Tratamiento";
            _cambiarVista = cambiarVista;
            _tratamientoRepo = new TratamientoRepository();
            _catalogoRepo = new CatalogoRepository();

            _nuevoTratamiento = new TratamientoPacienteModel
            {
                IdPaciente = idPaciente,
                IdDoctor = 1, // ID por defecto del doctor principal
                FechaInicio = DateTime.Now,
                Estado = "En progreso",
                Observaciones = string.Empty // Aquí se almacenará el Plan de Tratamiento
            };

            GuardarCommand = new RelayCommand(async (param) => await GuardarAsync());
            CancelarCommand = new RelayCommand(VolverLista);

            _ = CargarCatalogoAsync();
        }

        private async Task CargarCatalogoAsync()
        {
            try
            {
                var catalogo = await _catalogoRepo.ObtenerTratamientosActivosAsync();
                ListaCatalogo = new ObservableCollection<CatalogoTratamientosModel>(catalogo);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al cargar catálogo de tratamientos: " + ex.Message;
            }
        }

        private async Task GuardarAsync()
        {
            MensajeError = string.Empty;

            if (TratamientoSeleccionado == null)
            {
                MensajeError = "Debe seleccionar un tratamiento del catálogo.";
                return;
            }

            if (NuevoTratamiento.CostoTotal <= 0)
            {
                MensajeError = "El costo acordado debe ser mayor a cero.";
                return;
            }

            EstaCargando = true;
            try
            {
                NuevoTratamiento.IdTratamiento = TratamientoSeleccionado.IdTratamiento;

                // Si en tu repositorio el método se llama AsignarTratamientoAsync o CrearTratamientoAsync,
                // asegúrate de usar el nombre correspondiente:
                await _tratamientoRepo.CrearTratamientoAsync(NuevoTratamiento);

                MessageBox.Show("¡Tratamiento asignado con éxito a la cuenta del paciente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                VolverLista(null);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al guardar el tratamiento: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void VolverLista(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaPrincipal = new TratamientosView();
                vistaPrincipal.DataContext = new TratamientosViewModel(_cambiarVista);
                _cambiarVista(vistaPrincipal);
            }
        }
    }
}