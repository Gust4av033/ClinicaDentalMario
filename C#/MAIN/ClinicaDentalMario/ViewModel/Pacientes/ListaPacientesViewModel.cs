using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class ListaPacientesViewModel : ViewModelBase
    {
        private readonly PacienteRepository _pacienteRepository;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly Action<object> _cambiarVista;
        private CancellationTokenSource? _busquedaCts;
        private int _versionCarga;

        private ObservableCollection<PacienteModel> _pacientes = new();
        public ObservableCollection<PacienteModel> Pacientes
        {
            get => _pacientes;
            private set
            {
                if (SetProperty(ref _pacientes, value))
                {
                    OnPropertyChanged(nameof(CantidadPacientes));
                    OnPropertyChanged(nameof(SinResultados));
                }
            }
        }

        public int CantidadPacientes => Pacientes.Count;
        public bool SinResultados => !EstaCargando && Pacientes.Count == 0;

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set
            {
                if (SetProperty(ref _terminoBusqueda, value))
                {
                    ProgramarBusqueda();
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
                    _ = CargarSegunFiltroAsync();
                }
            }
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set => SetProperty(ref _pacienteSeleccionado, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand BuscarCommand { get; }
        public AsyncRelayCommand RecargarCommand { get; }
        public ICommand NuevoPacienteCommand { get; }
        public ICommand EditarPacienteCommand { get; }
        public ICommand VerHistorialCommand { get; }

        public ListaPacientesViewModel(Action<object> cambiarVistaAccion)
            : this(
                cambiarVistaAccion,
                new PacienteRepository(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public ListaPacientesViewModel(
            Action<object> cambiarVistaAccion,
            PacienteRepository pacienteRepository,
            IExceptionHandler exceptionHandler)
        {
            _cambiarVista = cambiarVistaAccion ?? throw new ArgumentNullException(nameof(cambiarVistaAccion));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Pacientes";

            BuscarCommand = new AsyncRelayCommand(_ => CargarSegunFiltroAsync());
            RecargarCommand = new AsyncRelayCommand(_ => CargarSegunFiltroAsync());
            NuevoPacienteCommand = new RelayCommand(AbrirNuevoPaciente);
            EditarPacienteCommand = new RelayCommand(AbrirEditarPaciente);
            VerHistorialCommand = new RelayCommand(AbrirHistorialPaciente);

            _ = CargarSegunFiltroAsync();
        }

        private void ProgramarBusqueda()
        {
            _busquedaCts?.Cancel();
            _busquedaCts?.Dispose();
            _busquedaCts = new CancellationTokenSource();
            _ = BuscarConEsperaAsync(_busquedaCts.Token);
        }

        private async Task BuscarConEsperaAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(300, cancellationToken);
                await CargarSegunFiltroAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task CargarSegunFiltroAsync()
        {
            int versionActual = Interlocked.Increment(ref _versionCarga);

            MensajeError = string.Empty;
            EstaCargando = true;
            OnPropertyChanged(nameof(SinResultados));

            try
            {
                IEnumerable<PacienteModel> lista;
                string termino = TerminoBusqueda.Trim();
                bool mostrarInactivos = MostrarInactivos;

                if (string.IsNullOrWhiteSpace(termino))
                {
                    lista = mostrarInactivos
                        ? await _pacienteRepository.ObtenerInactivosAsync()
                        : await _pacienteRepository.ObtenerTodosAsync();
                }
                else
                {
                    lista = await _pacienteRepository.BuscarAsync(
                        termino,
                        soloInactivos: mostrarInactivos);
                }

                // Si comenzó otra carga mientras esperábamos la BD,
                // esta respuesta ya está obsoleta y no debe reemplazar la más reciente.
                if (versionActual != Volatile.Read(ref _versionCarga))
                {
                    return;
                }

                Pacientes = new ObservableCollection<PacienteModel>(lista);
            }
            catch (Exception ex)
            {
                if (versionActual != Volatile.Read(ref _versionCarga))
                {
                    return;
                }

                Pacientes = new ObservableCollection<PacienteModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los pacientes.");
            }
            finally
            {
                if (versionActual == Volatile.Read(ref _versionCarga))
                {
                    EstaCargando = false;
                    OnPropertyChanged(nameof(SinResultados));
                }
            }
        }

        private void AbrirNuevoPaciente(object? parameter)
        {
            var vista = new NuevoPacienteView
            {
                DataContext = new NuevoPacienteViewModel(_cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void AbrirEditarPaciente(object? parameter)
        {
            var paciente = parameter as PacienteModel ?? PacienteSeleccionado;
            if (paciente is null)
            {
                return;
            }

            var vista = new EditarPacienteView
            {
                DataContext = new EditarPacienteViewModel(paciente, _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void AbrirHistorialPaciente(object? parameter)
        {
            var paciente = parameter as PacienteModel ?? PacienteSeleccionado;
            if (paciente is null)
            {
                return;
            }

            var vista = new HistorialPacienteView
            {
                DataContext = new HistorialPacienteViewModel(paciente, _cambiarVista)
            };

            _cambiarVista(vista);
        }
    }
}
