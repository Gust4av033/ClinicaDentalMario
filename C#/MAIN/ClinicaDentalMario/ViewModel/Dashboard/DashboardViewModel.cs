using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Navigation;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Dashboard
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        private readonly DashboardRepository _dashboardRepo;
        private readonly Action<object> _cambiarVista;
        private readonly IShellViewFactory _viewFactory;
        private readonly IPermissionService _permissionService;
        private readonly CultureInfo _cultura = CultureInfo.GetCultureInfo("es-SV");

        private string _saludoDinamico = string.Empty;
        public string SaludoDinamico
        {
            get => _saludoDinamico;
            private set => SetProperty(ref _saludoDinamico, value);
        }

        private string _fechaHoraActual = string.Empty;
        public string FechaHoraActual
        {
            get => _fechaHoraActual;
            private set => SetProperty(ref _fechaHoraActual, value);
        }

        private string _ultimaActualizacion = string.Empty;
        public string UltimaActualizacion
        {
            get => _ultimaActualizacion;
            private set => SetProperty(ref _ultimaActualizacion, value);
        }

        private decimal _ingresosHoy;
        public decimal IngresosHoy
        {
            get => _ingresosHoy;
            private set => SetProperty(ref _ingresosHoy, value);
        }

        private decimal _ingresosMes;
        public decimal IngresosMes
        {
            get => _ingresosMes;
            private set => SetProperty(ref _ingresosMes, value);
        }

        private int _pacientesActivos;
        public int PacientesActivos
        {
            get => _pacientesActivos;
            private set => SetProperty(ref _pacientesActivos, value);
        }

        private int _pacientesNuevosMes;
        public int PacientesNuevosMes
        {
            get => _pacientesNuevosMes;
            private set => SetProperty(ref _pacientesNuevosMes, value);
        }

        private int _citasHoy;
        public int CitasHoy
        {
            get => _citasHoy;
            private set => SetProperty(ref _citasHoy, value);
        }

        private int _citasPendientes;
        public int CitasPendientes
        {
            get => _citasPendientes;
            private set => SetProperty(ref _citasPendientes, value);
        }

        private int _citasConfirmadas;
        public int CitasConfirmadas
        {
            get => _citasConfirmadas;
            private set => SetProperty(ref _citasConfirmadas, value);
        }

        private int _citasAtendidas;
        public int CitasAtendidas
        {
            get => _citasAtendidas;
            private set => SetProperty(ref _citasAtendidas, value);
        }

        private int _tratamientosActivos;
        public int TratamientosActivos
        {
            get => _tratamientosActivos;
            private set => SetProperty(ref _tratamientosActivos, value);
        }

        private int _tratamientosPendientes;
        public int TratamientosPendientes
        {
            get => _tratamientosPendientes;
            private set => SetProperty(ref _tratamientosPendientes, value);
        }

        private int _tratamientosEnProgreso;
        public int TratamientosEnProgreso
        {
            get => _tratamientosEnProgreso;
            private set => SetProperty(ref _tratamientosEnProgreso, value);
        }

        private decimal _saldoPendiente;
        public decimal SaldoPendiente
        {
            get => _saldoPendiente;
            private set => SetProperty(ref _saldoPendiente, value);
        }

        private DashboardCitaModel? _proximaCita;
        public DashboardCitaModel? ProximaCita
        {
            get => _proximaCita;
            private set
            {
                if (SetProperty(ref _proximaCita, value))
                {
                    OnPropertyChanged(nameof(TieneProximaCita));
                }
            }
        }

        public bool TieneProximaCita => ProximaCita is not null;

        public string RolActual => UsuarioActual.NombreRol;

        public bool PuedeVerPacientes => _permissionService.TienePermiso(PermisoSistema.GestionarPacientes);
        public bool PuedeVerAgenda => _permissionService.TienePermiso(PermisoSistema.GestionarAgenda);
        public bool PuedeVerTratamientos => _permissionService.TienePermiso(PermisoSistema.GestionarTratamientos);
        public bool PuedeVerPagos => _permissionService.TienePermiso(PermisoSistema.GestionarPagos);
        public bool PuedeVerReportes => _permissionService.TienePermiso(PermisoSistema.VerReportes);
        public bool PuedeVerConfiguracion => _permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion);

        public ObservableCollection<DashboardCitaModel> ListaCitasHoy { get; } = new();
        public ObservableCollection<DashboardSaldoPacienteModel> TopSaldosPendientes { get; } = new();

        public ICommand RecargarCommand { get; }
        public ICommand IrPacientesCommand { get; }
        public ICommand IrAgendaCommand { get; }
        public ICommand IrTratamientosCommand { get; }
        public ICommand IrPagosCommand { get; }
        public ICommand IrReportesCommand { get; }
        public ICommand AbrirConfiguracionCommand { get; }

        public DashboardViewModel(Action<object> cambiarVista)
        {
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _dashboardRepo = new DashboardRepository();
            _viewFactory = new ShellViewFactory();
            _permissionService = new PermissionService();

            Titulo = "Dashboard";

            RecargarCommand = new AsyncRelayCommand(
                async _ => await CargarDatosDashboardAsync(),
                _ => !EstaCargando);

            IrPacientesCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarPacientes,
                () => _viewFactory.CrearPacientes(_cambiarVista));

            IrAgendaCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarAgenda,
                () => _viewFactory.CrearAgenda(_cambiarVista));

            IrTratamientosCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarTratamientos,
                () => _viewFactory.CrearTratamientos(_cambiarVista));

            IrPagosCommand = CrearComandoNavegacion(
                PermisoSistema.GestionarPagos,
                () => _viewFactory.CrearPagos(_cambiarVista));

            IrReportesCommand = CrearComandoNavegacion(
                PermisoSistema.VerReportes,
                () => _viewFactory.CrearReportes(_cambiarVista));

            AbrirConfiguracionCommand = CrearComandoNavegacion(
                PermisoSistema.AdministrarConfiguracion,
                _viewFactory.CrearConfiguracion);

            ActualizarEncabezado();
            _ = CargarDatosDashboardAsync();
        }

        public async Task CargarDatosDashboardAsync()
        {
            LimpiarMensaje();
            ActualizarEncabezado();

            try
            {
                await EjecutarConCargaAsync(async () =>
                {
                    DateTime ahora = DateTime.Now;

                    Task<DashboardResumenModel> resumenTask = _dashboardRepo.ObtenerResumenAsync(ahora.Date);
                    Task<IReadOnlyList<DashboardCitaModel>> citasTask = _dashboardRepo.ObtenerCitasDelDiaAsync(ahora.Date);
                    Task<IReadOnlyList<DashboardSaldoPacienteModel>> saldosTask = _dashboardRepo.ObtenerSaldosPendientesAsync();
                    Task<DashboardCitaModel?> proximaTask = _dashboardRepo.ObtenerProximaCitaAsync(ahora);

                    await Task.WhenAll(resumenTask, citasTask, saldosTask, proximaTask);

                    DashboardResumenModel resumen = await resumenTask;
                    AplicarResumen(resumen);

                    ListaCitasHoy.Clear();
                    foreach (DashboardCitaModel cita in await citasTask)
                    {
                        ListaCitasHoy.Add(cita);
                    }

                    TopSaldosPendientes.Clear();
                    foreach (DashboardSaldoPacienteModel saldo in await saldosTask)
                    {
                        TopSaldosPendientes.Add(saldo);
                    }

                    ProximaCita = await proximaTask;
                    UltimaActualizacion = DateTime.Now.ToString("hh:mm tt", _cultura);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar dashboard: {ex}");
                MostrarError("No fue posible actualizar el Dashboard. Verifica la conexión y vuelve a intentar.");
            }
        }

        private void AplicarResumen(DashboardResumenModel resumen)
        {
            IngresosHoy = resumen.IngresosHoy;
            IngresosMes = resumen.IngresosMes;
            PacientesActivos = resumen.PacientesActivos;
            PacientesNuevosMes = resumen.PacientesNuevosMes;
            CitasHoy = resumen.CitasHoy;
            CitasPendientes = resumen.CitasPendientes;
            CitasConfirmadas = resumen.CitasConfirmadas;
            CitasAtendidas = resumen.CitasAtendidas;
            TratamientosActivos = resumen.TratamientosActivos;
            TratamientosPendientes = resumen.TratamientosPendientes;
            TratamientosEnProgreso = resumen.TratamientosEnProgreso;
            SaldoPendiente = resumen.SaldoPendiente;
        }

        private void ActualizarEncabezado()
        {
            DateTime ahora = DateTime.Now;
            string nombre = ObtenerNombreCorto();

            SaludoDinamico = ahora.Hour switch
            {
                >= 5 and < 12 => $"¡Buenos días, {nombre}!",
                >= 12 and < 19 => $"¡Buenas tardes, {nombre}!",
                _ => $"¡Buenas noches, {nombre}!"
            };

            FechaHoraActual = ahora.ToString("dddd, dd 'de' MMMM 'de' yyyy  •  hh:mm tt", _cultura);
        }

        private static string ObtenerNombreCorto()
        {
            string nombre = string.IsNullOrWhiteSpace(UsuarioActual.NombreCompleto)
                ? UsuarioActual.NombreUsuario
                : UsuarioActual.NombreCompleto;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return "usuario";
            }

            return nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;
        }

        private RelayCommand CrearComandoNavegacion(PermisoSistema permiso, Func<object> crearVista)
        {
            return new RelayCommand(
                _ =>
                {
                    if (_permissionService.TienePermiso(permiso))
                    {
                        _cambiarVista(crearVista());
                    }
                },
                _ => _permissionService.TienePermiso(permiso));
        }
    }
}
