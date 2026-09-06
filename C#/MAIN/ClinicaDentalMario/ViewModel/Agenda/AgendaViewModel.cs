using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class AgendaViewModel : ViewModelBase
    {
        private const string ModoDia = "Dia";
        private const string ModoSemana = "Semana";
        private const string ModoTodas = "Todas";

        private readonly Action<object> _cambiarVista;
        private readonly CitaRepository _citaRepository;
        private readonly DoctorRepository _doctorRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private bool _filtrosCargados;
        private bool _suspenderRecarga;

        private ObservableCollection<AgendaCitaModel> _citasDelDia = new();
        public ObservableCollection<AgendaCitaModel> CitasDelDia
        {
            get => _citasDelDia;
            private set
            {
                if (SetProperty(ref _citasDelDia, value))
                {
                    OnPropertyChanged(nameof(SinCitas));
                    OnPropertyChanged(nameof(CantidadCitas));
                    OnPropertyChanged(nameof(CantidadPendientes));
                    OnPropertyChanged(nameof(CantidadConfirmadas));
                }
            }
        }

        public bool SinCitas => !EstaCargando && CitasDelDia.Count == 0;
        public int CantidadCitas => CitasDelDia.Count;
        public int CantidadPendientes => CitasDelDia.Count(x => x.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase));
        public int CantidadConfirmadas => CitasDelDia.Count(x => x.Estado.Equals("Confirmada", StringComparison.OrdinalIgnoreCase));

        private DateTime _fechaSeleccionada;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (SetProperty(ref _fechaSeleccionada, value.Date))
                {
                    NotificarPeriodo();
                    CerrarDetalle();

                    if (_filtrosCargados && !_suspenderRecarga && !EsVistaTodas)
                        _ = CargarAgendaAsync();
                }
            }
        }

        private string _modoVista = ModoDia;
        public string ModoVista
        {
            get => _modoVista;
            private set
            {
                if (SetProperty(ref _modoVista, value))
                {
                    NotificarPeriodo();
                    DiaAnteriorCommand.NotificarCanExecuteChanged();
                    DiaSiguienteCommand.NotificarCanExecuteChanged();
                }
            }
        }

        public bool EsVistaDia => ModoVista == ModoDia;
        public bool EsVistaSemana => ModoVista == ModoSemana;
        public bool EsVistaTodas => ModoVista == ModoTodas;
        public bool MostrarFechaEnListado => !EsVistaDia;

        public string FechaSeleccionadaTexto
        {
            get
            {
                if (EsVistaTodas)
                    return "Todas las citas registradas";

                if (EsVistaSemana)
                {
                    DateTime inicio = ObtenerInicioSemana(FechaSeleccionada);
                    DateTime fin = inicio.AddDays(6);
                    return $"Semana del {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}";
                }

                return FechaSeleccionada.ToString(
                    "dddd, dd 'de' MMMM 'de' yyyy",
                    CultureInfo.CurrentCulture);
            }
        }

        public bool EsHoy => EsVistaDia && FechaSeleccionada.Date == DateTime.Today;

        private ObservableCollection<DoctorModel> _doctoresFiltro = new();
        public ObservableCollection<DoctorModel> DoctoresFiltro
        {
            get => _doctoresFiltro;
            private set => SetProperty(ref _doctoresFiltro, value);
        }

        private ObservableCollection<EstadoCitaModel> _estadosFiltro = new();
        public ObservableCollection<EstadoCitaModel> EstadosFiltro
        {
            get => _estadosFiltro;
            private set => SetProperty(ref _estadosFiltro, value);
        }

        private DoctorModel? _doctorFiltroSeleccionado;
        public DoctorModel? DoctorFiltroSeleccionado
        {
            get => _doctorFiltroSeleccionado;
            set
            {
                if (SetProperty(ref _doctorFiltroSeleccionado, value) &&
                    _filtrosCargados && !_suspenderRecarga)
                {
                    _ = CargarAgendaAsync();
                }
            }
        }

        private EstadoCitaModel? _estadoFiltroSeleccionado;
        public EstadoCitaModel? EstadoFiltroSeleccionado
        {
            get => _estadoFiltroSeleccionado;
            set
            {
                if (SetProperty(ref _estadoFiltroSeleccionado, value) &&
                    _filtrosCargados && !_suspenderRecarga)
                {
                    _ = CargarAgendaAsync();
                }
            }
        }

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set => SetProperty(ref _terminoBusqueda, value ?? string.Empty);
        }

        private AgendaCitaModel? _citaSeleccionada;
        public AgendaCitaModel? CitaSeleccionada
        {
            get => _citaSeleccionada;
            set
            {
                if (SetProperty(ref _citaSeleccionada, value))
                {
                    PanelDetalleVisibility = value is null
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    AnchoPanelDetalle = value is null ? 0 : 340;
                    NotificarComandosSeleccion();
                }
            }
        }

        private Visibility _panelDetalleVisibility = Visibility.Collapsed;
        public Visibility PanelDetalleVisibility
        {
            get => _panelDetalleVisibility;
            private set => SetProperty(ref _panelDetalleVisibility, value);
        }

        private double _anchoPanelDetalle;
        public double AnchoPanelDetalle
        {
            get => _anchoPanelDetalle;
            private set => SetProperty(ref _anchoPanelDetalle, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public RelayCommand NuevaCitaCommand { get; }
        public RelayCommand EditarCitaCommand { get; }
        public AsyncRelayCommand ConfirmarCitaCommand { get; }
        public AsyncRelayCommand CancelarCitaCommand { get; }
        public AsyncRelayCommand FinalizarCitaCommand { get; }
        public AsyncRelayCommand NoAsistioCommand { get; }
        public RelayCommand CerrarDetalleCommand { get; }
        public AsyncRelayCommand RecargarCommand { get; }
        public AsyncRelayCommand BuscarCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }
        public RelayCommand MostrarDiaCommand { get; }
        public RelayCommand MostrarSemanaCommand { get; }
        public RelayCommand MostrarTodasCommand { get; }
        public RelayCommand DiaAnteriorCommand { get; }
        public RelayCommand IrHoyCommand { get; }
        public RelayCommand DiaSiguienteCommand { get; }

        public AgendaViewModel(Action<object> cambiarVista, DateTime? fechaInicial = null)
            : this(
                cambiarVista,
                fechaInicial,
                new CitaRepository(),
                new DoctorRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public AgendaViewModel(
            Action<object> cambiarVista,
            DateTime? fechaInicial,
            CitaRepository citaRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
            : this(
                cambiarVista,
                fechaInicial,
                citaRepository,
                new DoctorRepository(),
                messageService,
                exceptionHandler)
        {
        }

        public AgendaViewModel(
            Action<object> cambiarVista,
            DateTime? fechaInicial,
            CitaRepository citaRepository,
            DoctorRepository doctorRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _citaRepository = citaRepository ?? throw new ArgumentNullException(nameof(citaRepository));
            _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _fechaSeleccionada = fechaInicial?.Date ?? DateTime.Today;

            Titulo = "Agenda de Citas";
            NuevaCitaCommand = new RelayCommand(_ => AbrirNuevaCita());
            EditarCitaCommand = new RelayCommand(_ => AbrirEditarCita(), _ => PuedeReprogramarOCancelar());
            ConfirmarCitaCommand = new AsyncRelayCommand(_ => ConfirmarCitaAsync(), _ => PuedeConfirmar());
            CancelarCitaCommand = new AsyncRelayCommand(_ => CancelarCitaAsync(), _ => PuedeReprogramarOCancelar());
            FinalizarCitaCommand = new AsyncRelayCommand(_ => FinalizarCitaAsync(), _ => PuedeMarcarAtendida());
            NoAsistioCommand = new AsyncRelayCommand(_ => MarcarNoAsistioAsync(), _ => PuedeMarcarNoAsistio());
            CerrarDetalleCommand = new RelayCommand(_ => CerrarDetalle());
            RecargarCommand = new AsyncRelayCommand(_ => CargarAgendaAsync());
            BuscarCommand = new AsyncRelayCommand(_ => CargarAgendaAsync());
            LimpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());
            MostrarDiaCommand = new RelayCommand(_ => CambiarModo(ModoDia));
            MostrarSemanaCommand = new RelayCommand(_ => CambiarModo(ModoSemana));
            MostrarTodasCommand = new RelayCommand(_ => CambiarModo(ModoTodas));
            DiaAnteriorCommand = new RelayCommand(_ => MoverPeriodo(-1), _ => !EsVistaTodas);
            IrHoyCommand = new RelayCommand(_ => IrHoy());
            DiaSiguienteCommand = new RelayCommand(_ => MoverPeriodo(1), _ => !EsVistaTodas);

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;
            OnPropertyChanged(nameof(SinCitas));

            try
            {
                var doctores = await _doctorRepository.ObtenerDoctoresActivosAsync();
                var estados = await _citaRepository.ObtenerEstadosAsync();

                var todosDoctores = new DoctorModel
                {
                    IdDoctor = 0,
                    NombreCompleto = "Todos los doctores",
                    Activo = true
                };

                var todosEstados = new EstadoCitaModel
                {
                    IdEstado = 0,
                    Nombre = "Todos los estados"
                };

                DoctoresFiltro = new ObservableCollection<DoctorModel>(
                    new[] { todosDoctores }.Concat(doctores));
                EstadosFiltro = new ObservableCollection<EstadoCitaModel>(
                    new[] { todosEstados }.Concat(estados));

                _suspenderRecarga = true;
                DoctorFiltroSeleccionado = todosDoctores;
                EstadoFiltroSeleccionado = todosEstados;
                _suspenderRecarga = false;
                _filtrosCargados = true;
            }
            catch (Exception ex)
            {
                _filtrosCargados = false;
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los filtros de la agenda.");
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinCitas));
            }

            if (_filtrosCargados)
                await CargarAgendaAsync();
        }

        private async Task CargarAgendaAsync()
        {
            if (!_filtrosCargados)
                return;

            MensajeError = string.Empty;
            EstaCargando = true;
            OnPropertyChanged(nameof(SinCitas));
            CerrarDetalle();

            try
            {
                DateTime? fechaDesde = null;
                DateTime? fechaHasta = null;

                if (EsVistaDia)
                {
                    fechaDesde = FechaSeleccionada.Date;
                    fechaHasta = fechaDesde.Value.AddDays(1);
                }
                else if (EsVistaSemana)
                {
                    fechaDesde = ObtenerInicioSemana(FechaSeleccionada);
                    fechaHasta = fechaDesde.Value.AddDays(7);
                }

                int? idDoctor = DoctorFiltroSeleccionado is { IdDoctor: > 0 }
                    ? DoctorFiltroSeleccionado.IdDoctor
                    : null;
                int? idEstado = EstadoFiltroSeleccionado is { IdEstado: > 0 }
                    ? EstadoFiltroSeleccionado.IdEstado
                    : null;

                var citas = await _citaRepository.ObtenerCitasAsync(
                    fechaDesde,
                    fechaHasta,
                    idDoctor,
                    idEstado,
                    TerminoBusqueda);

                CitasDelDia = new ObservableCollection<AgendaCitaModel>(citas);
            }
            catch (Exception ex)
            {
                CitasDelDia = new ObservableCollection<AgendaCitaModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar la agenda.");
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinCitas));
                NotificarComandosSeleccion();
            }
        }

        private void CambiarModo(string modo)
        {
            if (ModoVista == modo)
                return;

            ModoVista = modo;
            CerrarDetalle();

            if (_filtrosCargados)
                _ = CargarAgendaAsync();
        }

        private void MoverPeriodo(int direccion)
        {
            if (EsVistaTodas)
                return;

            int dias = EsVistaSemana ? 7 : 1;
            FechaSeleccionada = FechaSeleccionada.AddDays(dias * direccion);
        }

        private void IrHoy()
        {
            bool requiereRecarga = !EsVistaDia || FechaSeleccionada.Date != DateTime.Today;

            _suspenderRecarga = true;
            ModoVista = ModoDia;
            FechaSeleccionada = DateTime.Today;
            _suspenderRecarga = false;

            if (requiereRecarga && _filtrosCargados)
                _ = CargarAgendaAsync();
        }

        private void LimpiarFiltros()
        {
            _suspenderRecarga = true;
            TerminoBusqueda = string.Empty;
            DoctorFiltroSeleccionado = DoctoresFiltro.FirstOrDefault();
            EstadoFiltroSeleccionado = EstadosFiltro.FirstOrDefault();
            _suspenderRecarga = false;

            if (_filtrosCargados)
                _ = CargarAgendaAsync();
        }

        private static DateTime ObtenerInicioSemana(DateTime fecha)
        {
            int diferencia = (7 + (int)fecha.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            return fecha.Date.AddDays(-diferencia);
        }

        private void NotificarPeriodo()
        {
            OnPropertyChanged(nameof(FechaSeleccionadaTexto));
            OnPropertyChanged(nameof(EsHoy));
            OnPropertyChanged(nameof(EsVistaDia));
            OnPropertyChanged(nameof(EsVistaSemana));
            OnPropertyChanged(nameof(EsVistaTodas));
            OnPropertyChanged(nameof(MostrarFechaEnListado));
        }

        private void AbrirNuevaCita()
        {
            var vista = new NuevaCitaView
            {
                DataContext = new NuevaCitaViewModel(_cambiarVista, FechaSeleccionada)
            };
            _cambiarVista(vista);
        }

        private void AbrirEditarCita()
        {
            if (!PuedeReprogramarOCancelar())
            {
                _messageService.MostrarAdvertencia(
                    "Solo las citas futuras pendientes o confirmadas pueden reprogramarse.",
                    "Cita no editable");
                return;
            }

            var vista = new EditarCitaView
            {
                DataContext = new EditarCitaViewModel(CitaSeleccionada!, _cambiarVista)
            };
            _cambiarVista(vista);
        }

        private async Task ConfirmarCitaAsync()
        {
            if (!PuedeConfirmar())
                return;

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Confirmas la cita de {cita.Paciente} para {cita.HorarioTexto}?",
                    "Confirmar cita"))
                return;

            await CambiarEstadoSeleccionadaAsync(
                "Confirmada",
                "La cita fue confirmada correctamente.",
                "Cita confirmada");
        }

        private async Task CancelarCitaAsync()
        {
            if (!PuedeReprogramarOCancelar())
                return;

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Deseas cancelar la cita de {cita.Paciente}?",
                    "Cancelar cita"))
                return;

            await CambiarEstadoSeleccionadaAsync(
                "Cancelada",
                "La cita fue cancelada correctamente.",
                "Cita cancelada");
        }

        private async Task FinalizarCitaAsync()
        {
            if (!PuedeMarcarAtendida())
                return;

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Confirmas que {cita.Paciente} fue atendido?",
                    "Marcar como atendida"))
                return;

            await CambiarEstadoSeleccionadaAsync(
                "Atendida",
                "La cita fue marcada como atendida.",
                "Cita atendida");
        }

        private async Task MarcarNoAsistioAsync()
        {
            if (!PuedeMarcarNoAsistio())
                return;

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Confirmas que {cita.Paciente} no asistió a la cita?",
                    "Marcar inasistencia"))
                return;

            await CambiarEstadoSeleccionadaAsync(
                "No Asistió",
                "La cita fue marcada como no asistida.",
                "Inasistencia registrada");
        }

        private async Task CambiarEstadoSeleccionadaAsync(
            string estado,
            string mensajeExito,
            string tituloExito)
        {
            AgendaCitaModel cita = CitaSeleccionada!;

            try
            {
                await _citaRepository.CambiarEstadoCitaAsync(cita.IdCita, estado);
                _messageService.MostrarExito(mensajeExito, tituloExito);
                CerrarDetalle();
                await CargarAgendaAsync();
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar el estado de la cita.");
            }
        }

        private bool PuedeConfirmar()
        {
            return CitaSeleccionada is not null &&
                   CitaSeleccionada.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase) &&
                   CitaSeleccionada.FechaHora > DateTime.Now;
        }

        private bool PuedeReprogramarOCancelar()
        {
            return CitaSeleccionada is not null &&
                   !CitaSeleccionada.EstaCerrada &&
                   CitaSeleccionada.FechaHora > DateTime.Now;
        }

        private bool PuedeMarcarAtendida()
        {
            return CitaSeleccionada is not null &&
                   !CitaSeleccionada.EstaCerrada &&
                   CitaSeleccionada.FechaHora <= DateTime.Now;
        }

        private bool PuedeMarcarNoAsistio()
        {
            return CitaSeleccionada is not null &&
                   !CitaSeleccionada.EstaCerrada &&
                   CitaSeleccionada.FechaHoraFin <= DateTime.Now;
        }

        private void NotificarComandosSeleccion()
        {
            EditarCitaCommand.NotificarCanExecuteChanged();
            ConfirmarCitaCommand.NotificarCanExecuteChanged();
            CancelarCitaCommand.NotificarCanExecuteChanged();
            FinalizarCitaCommand.NotificarCanExecuteChanged();
            NoAsistioCommand.NotificarCanExecuteChanged();
        }

        private void CerrarDetalle()
        {
            CitaSeleccionada = null;
        }
    }
}
