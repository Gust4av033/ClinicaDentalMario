using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class ListaPacientesViewModel : ViewModelBase
    {
        private readonly PacienteRepository _pacienteRepository;

        // Colección que notifica a la tabla de WPF cuando hay cambios
        private ObservableCollection<PacienteModel> _pacientes = new ObservableCollection<PacienteModel>();
        public ObservableCollection<PacienteModel> Pacientes
        {
            get => _pacientes;
            set => SetProperty(ref _pacientes, value);
        }

        // CORRECCIÓN: Se cambió a "TerminoBusqueda" para que haga match exacto con el XAML
        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set
            {
                if (SetProperty(ref _terminoBusqueda, value))
                {
                    // Búsqueda en tiempo real conforme la recepcionista escribe
                    _ = BuscarAsync();
                }
            }
        }

        private bool _mostrarInactivos;
        public bool MostrarInactivos
        {
            get => _mostrarInactivos;
            set
            {
                if (SetProperty(ref _mostrarInactivos, value))
                {
                    _ = CargarPacientesAsync(); // Si tocan el botón, recargamos la lista
                }
            }
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set => SetProperty(ref _pacienteSeleccionado, value);
        }

        // Delegado para cambiar de vista en el contenedor principal (MainViewModel)
        private readonly Action<object> _cambiarVista;

        // Comandos para los botones
        public ICommand BuscarCommand { get; }
        public ICommand NuevoPacienteCommand { get; }
        public ICommand EditarPacienteCommand { get; }
        public ICommand VerHistorialCommand { get; } // Comando para el ojito

        // Recibe la acción desde el MainViewModel
        public ListaPacientesViewModel(Action<object> cambiarVistaAccion)
        {
            Titulo = "Listado de Pacientes";
            _pacienteRepository = new PacienteRepository();
            _cambiarVista = cambiarVistaAccion; // Guardamos la referencia de navegación

            BuscarCommand = new RelayCommand(async (param) => await BuscarAsync());
            NuevoPacienteCommand = new RelayCommand(AbrirNuevoPaciente);
            EditarPacienteCommand = new RelayCommand(AbrirEditarPaciente);
            VerHistorialCommand = new RelayCommand(AbrirHistorialPaciente); // Inicializado

            _ = CargarPacientesAsync();
        }

        private async Task CargarPacientesAsync()
        {
            EstaCargando = true;
            try
            {
                // Alternamos entre la lista de activos y la de eliminados
                var lista = MostrarInactivos
                    ? await _pacienteRepository.ObtenerInactivosAsync()
                    : await _pacienteRepository.ObtenerTodosAsync();

                Pacientes = new ObservableCollection<PacienteModel>(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar pacientes: " + ex.Message);
            }
            finally { EstaCargando = false; }
        }

        private async Task BuscarAsync()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                await CargarPacientesAsync();
                return;
            }

            try
            {
                var resultados = await _pacienteRepository.BuscarAsync(TerminoBusqueda);
                Pacientes = new ObservableCollection<PacienteModel>(resultados);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al buscar pacientes: " + ex.Message);
            }
        }

        private void AbrirNuevoPaciente(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaNuevo = new NuevoPacienteView();

                // CORRECCIÓN: Ahora le pasamos el delegado _cambiarVista al constructor
                var viewModelNuevo = new NuevoPacienteViewModel(_cambiarVista);

                vistaNuevo.DataContext = viewModelNuevo;
                _cambiarVista(vistaNuevo);
            }
        }

        private void AbrirEditarPaciente(object? parameter)
        {
            var pacienteAEditar = parameter as PacienteModel ?? PacienteSeleccionado;

            if (pacienteAEditar != null && _cambiarVista != null)
            {
                var vistaEdicion = new EditarPacienteView();

                // ¡AQUÍ ESTÁ LA SOLUCIÓN! Le agregamos , _cambiarVista
                var viewModelEdicion = new EditarPacienteViewModel(pacienteAEditar, _cambiarVista);

                vistaEdicion.DataContext = viewModelEdicion;
                _cambiarVista(vistaEdicion);
            }
        }

        // De paso corregimos el del historial para que no te dé el mismo error
        private void AbrirHistorialPaciente(object? parameter)
        {
            var pacienteSeleccionado = parameter as PacienteModel ?? PacienteSeleccionado;

            if (pacienteSeleccionado != null && _cambiarVista != null)
            {
                var vistaHistorial = new HistorialPacienteView();

                // ¡AQUÍ TAMBIÉN LE AGREGAMOS LA NAVEGACIÓN!
                var viewModelHistorial = new HistorialPacienteViewModel(pacienteSeleccionado, _cambiarVista);

                vistaHistorial.DataContext = viewModelHistorial;
                _cambiarVista(vistaHistorial);
            }
        }


    }
}