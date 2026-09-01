using ClinicaDentalMario.Common;
using ClinicaDentalMario.Navigation;
using ClinicaDentalMario.Services;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Base
{
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IShellViewFactory _viewFactory;
        private readonly IPermissionService _permissionService;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private PermisoSistema? _moduloActual;

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

        public bool PuedeVerDashboard => TienePermiso(PermisoSistema.VerDashboard);
        public bool PuedeVerPacientes => TienePermiso(PermisoSistema.GestionarPacientes);
        public bool PuedeVerAgenda => TienePermiso(PermisoSistema.GestionarAgenda);
        public bool PuedeVerTratamientos => TienePermiso(PermisoSistema.GestionarTratamientos);
        public bool PuedeVerPagos => TienePermiso(PermisoSistema.GestionarPagos);
        public bool PuedeVerReportes => TienePermiso(PermisoSistema.VerReportes);
        public bool PuedeVerConfiguracion => TienePermiso(PermisoSistema.AdministrarConfiguracion);

        public ICommand NavegarDashboardCommand { get; }
        public ICommand NavegarPacientesCommand { get; }
        public ICommand NavegarAgendaCommand { get; }
        public ICommand NavegarTratamientosCommand { get; }
        public ICommand NavegarPagosCommand { get; }
        public ICommand NavegarReportesCommand { get; }
        public ICommand NavegarConfiguracionCommand { get; }
        public ICommand CerrarSesionCommand { get; }

        public event EventHandler? CierreSesionSolicitado;

        public MainViewModel()
            : this(
                new NavigationService(),
                new ShellViewFactory(),
                new PermissionService(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public MainViewModel(
            INavigationService navigationService,
            IShellViewFactory viewFactory,
            IPermissionService permissionService,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            _navigationService.VistaActualChanged += OnVistaActualChanged;

            NavegarDashboardCommand = CrearComandoNavegacion(
                PermisoSistema.VerDashboard,
                () => _viewFactory.CrearDashboard(CambiarVista),
                "Dashboard");

            NavegarPacientesCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarPacientes,
                () => _viewFactory.CrearPacientes(CambiarVista),
                "Pacientes");

            NavegarAgendaCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarAgenda,
                () => _viewFactory.CrearAgenda(CambiarVista),
                "Agenda");

            NavegarTratamientosCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarTratamientos,
                () => _viewFactory.CrearTratamientos(CambiarVista),
                "Tratamientos");

            NavegarPagosCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarPagos,
                () => _viewFactory.CrearPagos(CambiarVista),
                "Pagos y Cuentas");

            NavegarReportesCommand = CrearComandoNavegacion(
                PermisoSistema.VerReportes,
                () => _viewFactory.CrearReportes(CambiarVista),
                "Reportes");

            NavegarConfiguracionCommand = CrearComandoNavegacion(
                PermisoSistema.AdministrarConfiguracion,
                _viewFactory.CrearConfiguracion,
                "Configuración");

            CerrarSesionCommand = new RelayCommand(_ => CerrarSesion());

            NavegarInicial();
        }

        private RelayCommand CrearComandoNavegacion(
            PermisoSistema permiso,
            Func<object> crearVista,
            string nombreModulo)
        {
            return new RelayCommand(
                _ => NavegarSeguro(permiso, crearVista, nombreModulo),
                _ => TienePermiso(permiso));
        }

        private bool TienePermiso(PermisoSistema permiso)
        {
            return _permissionService.TienePermiso(permiso);
        }

        private void NavegarInicial()
        {
            if (PuedeVerDashboard)
            {
                NavegarSeguro(
                    PermisoSistema.VerDashboard,
                    () => _viewFactory.CrearDashboard(CambiarVista),
                    "Dashboard");
                return;
            }

            VistaActual = null;
        }

        private void NavegarSeguro(
            PermisoSistema permiso,
            Func<object> crearVista,
            string nombreModulo)
        {
            if (!TienePermiso(permiso))
            {
                _messageService.MostrarAdvertencia(
                    $"No tienes permisos para acceder a {nombreModulo}.",
                    "Acceso denegado");
                return;
            }

            if (_moduloActual == permiso)
            {
                return;
            }

            try
            {
                object vista = crearVista();
                _moduloActual = permiso;
                _navigationService.Navegar(vista);
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(
                    ex,
                    $"No fue posible abrir {nombreModulo}.");
            }
        }

        private void OnVistaActualChanged(object? sender, EventArgs e)
        {
            VistaActual = _navigationService.VistaActual;
        }

        private void CambiarVista(object nuevaVista)
        {
            ArgumentNullException.ThrowIfNull(nuevaVista);

            try
            {
                // Las sub-vistas pertenecen al módulo actual. Al marcarlas como navegación
                // interna permitimos volver a pulsar el módulo principal para regresar a su raíz.
                _moduloActual = null;
                _navigationService.Navegar(nuevaVista);
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(
                    ex,
                    "No fue posible cambiar de pantalla.");
            }
        }

        private void CerrarSesion()
        {
            if (!_messageService.Confirmar("¿Deseas cerrar la sesión actual?", "Cerrar sesión"))
            {
                return;
            }

            UsuarioActual.CerrarSesion();
            _navigationService.VistaActualChanged -= OnVistaActualChanged;
            CierreSesionSolicitado?.Invoke(this, EventArgs.Empty);
        }
    }
}
