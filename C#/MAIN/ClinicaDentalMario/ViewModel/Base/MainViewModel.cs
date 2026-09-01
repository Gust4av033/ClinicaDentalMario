using ClinicaDentalMario.Common;
using ClinicaDentalMario.Navigation;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Agenda;
using ClinicaDentalMario.ViewModel.Configuracion;
using ClinicaDentalMario.ViewModel.Dashboard;
using ClinicaDentalMario.ViewModel.Pacientes;
using ClinicaDentalMario.ViewModel.Pagos;
using ClinicaDentalMario.ViewModel.Reportes;
using ClinicaDentalMario.ViewModel.Tratamientos;
using ClinicaDentalMario.Views.Agenda;
using ClinicaDentalMario.Views.Configuracion;
using ClinicaDentalMario.Views.Dashboard;
using ClinicaDentalMario.Views.Pacientes;
using ClinicaDentalMario.Views.Pagos;
using ClinicaDentalMario.Views.Reportes;
using ClinicaDentalMario.Views.Tratamientos;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Base
{
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IPermissionService _permissionService;
        private readonly IMessageService _messageService;

        private object? _vistaActual;
        public object? VistaActual
        {
            get => _vistaActual;
            private set => SetProperty(ref _vistaActual, value);
        }

        public string NombreUsuarioActual =>
            string.IsNullOrWhiteSpace(UsuarioActual.NombreCompleto)
                ? UsuarioActual.NombreUsuario
                : UsuarioActual.NombreCompleto;

        public string RolActual => UsuarioActual.NombreRol;

        public bool PuedeVerConfiguracion =>
            _permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion);

        public ICommand NavegarDashboardCommand { get; }
        public ICommand NavegarPacientesCommand { get; }
        public ICommand NavegarAgendaCommand { get; }
        public ICommand NavegarTratamientosCommand { get; }
        public ICommand NavegarPagosCommand { get; }
        public ICommand NavegarOdontogramaCommand { get; }
        public ICommand NavegarReportesCommand { get; }
        public ICommand NavegarConfiguracionCommand { get; }
        public ICommand CerrarSesionCommand { get; }

        public event EventHandler? CierreSesionSolicitado;

        public MainViewModel()
            : this(
                new NavigationService(),
                new PermissionService(),
                new MessageService())
        {
        }

        public MainViewModel(
            INavigationService navigationService,
            IPermissionService permissionService,
            IMessageService messageService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            _navigationService.VistaActualChanged += OnVistaActualChanged;

            NavegarDashboardCommand = new RelayCommand(_ => CargarDashboard());
            NavegarPacientesCommand = new RelayCommand(_ => CargarPacientes());
            NavegarAgendaCommand = new RelayCommand(_ => CargarAgenda());
            NavegarTratamientosCommand = new RelayCommand(_ => CargarTratamientos());
            NavegarPagosCommand = new RelayCommand(_ => CargarPagos());
            NavegarOdontogramaCommand = new RelayCommand(_ => { /* Próximamente */ });
            NavegarReportesCommand = new RelayCommand(_ => CargarReportes());
            NavegarConfiguracionCommand = new RelayCommand(
                _ => CargarConfiguracion(),
                _ => PuedeVerConfiguracion);
            CerrarSesionCommand = new RelayCommand(_ => CerrarSesion());

            CargarDashboard();
        }

        private void OnVistaActualChanged(object? sender, EventArgs e)
        {
            VistaActual = _navigationService.VistaActual;
        }

        private void CargarDashboard()
        {
            var vista = new DashboardView
            {
                DataContext = new DashboardViewModel(CambiarVista)
            };

            _navigationService.Navegar(vista);
        }

        private void CargarPacientes()
        {
            var vista = new ListaPacientesView
            {
                DataContext = new ListaPacientesViewModel(CambiarVista)
            };

            _navigationService.Navegar(vista);
        }

        private void CargarAgenda()
        {
            var vista = new AgendaView
            {
                DataContext = new AgendaViewModel(CambiarVista)
            };

            _navigationService.Navegar(vista);
        }

        private void CargarTratamientos()
        {
            var vista = new TratamientosView
            {
                DataContext = new TratamientosViewModel(CambiarVista)
            };

            _navigationService.Navegar(vista);
        }

        private void CargarPagos()
        {
            var vista = new EstadoCuentaView
            {
                DataContext = new EstadoCuentaViewModel(CambiarVista)
            };

            _navigationService.Navegar(vista);
        }

        private void CargarReportes()
        {
            var vista = new ReportesView
            {
                DataContext = new ReportesViewModel(CambiarVista)
            };

            _navigationService.Navegar(vista);
        }

        private void CargarConfiguracion()
        {
            if (!PuedeVerConfiguracion)
            {
                _messageService.MostrarAdvertencia("No tienes permisos para acceder a Configuración.");
                return;
            }

            var vista = new ConfiguracionView
            {
                DataContext = new ConfiguracionViewModel()
            };

            _navigationService.Navegar(vista);
        }

        private void CerrarSesion()
        {
            if (!_messageService.Confirmar("¿Deseas cerrar la sesión actual?", "Cerrar sesión"))
            {
                return;
            }

            UsuarioActual.CerrarSesion();
            CierreSesionSolicitado?.Invoke(this, EventArgs.Empty);
        }

        private void CambiarVista(object nuevaVista)
        {
            _navigationService.Navegar(nuevaVista);
        }
    }
}
