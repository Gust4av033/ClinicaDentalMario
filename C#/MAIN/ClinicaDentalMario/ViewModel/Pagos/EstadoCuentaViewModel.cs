using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pagos;
using System.Collections.ObjectModel;

namespace ClinicaDentalMario.ViewModel.Pagos
{
    public class EstadoCuentaViewModel : ViewModelBase
    {
        private const string TodosLosEstados = "Todos";

        private readonly PacienteRepository _pacienteRepository;
        private readonly TratamientoRepository _tratamientoRepository;
        private readonly PagoRepository _pagoRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private List<PacienteModel> _pacientesTodos = new();
        private List<TratamientoPacienteModel> _tratamientosTodos = new();
        private List<PagoModel> _pagosTodos = new();
        private Dictionary<int, decimal> _totalesPagadosPorTratamiento = new();
        private int _versionCargaTratamientos;
        private int _versionCargaPagos;

        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            private set => SetProperty(ref _listaPacientes, value);
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set
            {
                if (!SetProperty(ref _pacienteSeleccionado, value))
                    return;

                Interlocked.Increment(ref _versionCargaTratamientos);
                Interlocked.Increment(ref _versionCargaPagos);
                LimpiarEstadoCuentaCompleto();
                NotificarEstadoVacio();
                NotificarComandos();

                if (value is not null)
                    _ = CargarTratamientosSeguroAsync(value.IdPaciente);
            }
        }

        private string _busquedaPaciente = string.Empty;
        public string BusquedaPaciente
        {
            get => _busquedaPaciente;
            set
            {
                if (SetProperty(ref _busquedaPaciente, value ?? string.Empty))
                    AplicarFiltroPacientes();
            }
        }

        private ObservableCollection<TratamientoPacienteModel> _listaTratamientos = new();
        public ObservableCollection<TratamientoPacienteModel> ListaTratamientos
        {
            get => _listaTratamientos;
            private set => SetProperty(ref _listaTratamientos, value);
        }

        private TratamientoPacienteModel? _tratamientoSeleccionado;
        public TratamientoPacienteModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (!SetProperty(ref _tratamientoSeleccionado, value))
                    return;

                Interlocked.Increment(ref _versionCargaPagos);
                PagoSeleccionado = null;
                MensajeError = string.Empty;

                if (value is null)
                {
                    LimpiarEstadoTratamiento();
                }
                else
                {
                    CostoTotal = value.CostoTotal;
                    TotalAbonado = _totalesPagadosPorTratamiento.TryGetValue(value.Id, out decimal total)
                        ? total
                        : 0m;
                    EstadoTratamiento = value.Estado ?? string.Empty;
                    _pagosTodos = new List<PagoModel>();
                    HistorialPagos = new ObservableCollection<PagoModel>();
                    _ = CargarPagosSeguroAsync(value);
                }

                OnPropertyChanged(nameof(PuedeRegistrarAbono));
                OnPropertyChanged(nameof(MensajeAccionPago));
                NotificarEstadoVacio();
                NotificarComandos();
            }
        }

        public ObservableCollection<string> EstadosFiltro { get; } = new()
        {
            TodosLosEstados,
            "Pendiente",
            "En progreso",
            "Finalizado",
            "Cancelado"
        };

        private string _estadoFiltroSeleccionado = TodosLosEstados;
        public string EstadoFiltroSeleccionado
        {
            get => _estadoFiltroSeleccionado;
            set
            {
                string estado = string.IsNullOrWhiteSpace(value) ? TodosLosEstados : value;
                if (SetProperty(ref _estadoFiltroSeleccionado, estado))
                    AplicarFiltroTratamientos();
            }
        }

        private string _busquedaTratamiento = string.Empty;
        public string BusquedaTratamiento
        {
            get => _busquedaTratamiento;
            set
            {
                if (SetProperty(ref _busquedaTratamiento, value ?? string.Empty))
                    AplicarFiltroTratamientos();
            }
        }

        private decimal _costoTotal;
        public decimal CostoTotal
        {
            get => _costoTotal;
            private set
            {
                if (SetProperty(ref _costoTotal, value))
                    NotificarResumenTratamiento();
            }
        }

        private decimal _totalAbonado;
        public decimal TotalAbonado
        {
            get => _totalAbonado;
            private set
            {
                if (SetProperty(ref _totalAbonado, value))
                    NotificarResumenTratamiento();
            }
        }

        public decimal SaldoPendiente => Math.Max(0m, CostoTotal - TotalAbonado);

        public double PorcentajePagado => CostoTotal <= 0m
            ? 0d
            : Math.Clamp((double)(TotalAbonado / CostoTotal) * 100d, 0d, 100d);

        private string _estadoTratamiento = string.Empty;
        public string EstadoTratamiento
        {
            get => _estadoTratamiento;
            private set
            {
                if (SetProperty(ref _estadoTratamiento, value))
                {
                    OnPropertyChanged(nameof(PuedeRegistrarAbono));
                    OnPropertyChanged(nameof(MensajeAccionPago));
                    NuevoAbonoCommand?.NotificarCanExecuteChanged();
                }
            }
        }

        public bool PuedeRegistrarAbono => TratamientoSeleccionado is not null
            && PuedeRecibirAbonos(TratamientoSeleccionado)
            && SaldoPendiente > 0m
            && !EstaCargando;

        public string MensajeAccionPago
        {
            get
            {
                if (TratamientoSeleccionado is null)
                    return string.Empty;

                if (TratamientoSeleccionado.EstaFinalizado)
                    return "Tratamiento finalizado: el historial queda disponible solo para consulta y recibos.";

                if (TratamientoSeleccionado.EstaCancelado)
                    return "Tratamiento cancelado: no se permiten nuevos abonos.";

                if (SaldoPendiente <= 0m)
                    return "Tratamiento liquidado: no existe saldo pendiente.";

                return string.Empty;
            }
        }

        private decimal _totalCargosPaciente;
        public decimal TotalCargosPaciente
        {
            get => _totalCargosPaciente;
            private set
            {
                if (SetProperty(ref _totalCargosPaciente, value))
                    OnPropertyChanged(nameof(SaldoTotalPaciente));
            }
        }

        private decimal _totalPagadoPaciente;
        public decimal TotalPagadoPaciente
        {
            get => _totalPagadoPaciente;
            private set
            {
                if (SetProperty(ref _totalPagadoPaciente, value))
                    OnPropertyChanged(nameof(SaldoTotalPaciente));
            }
        }

        public decimal SaldoTotalPaciente => Math.Max(0m, TotalCargosPaciente - TotalPagadoPaciente);

        private ObservableCollection<PagoModel> _historialPagos = new();
        public ObservableCollection<PagoModel> HistorialPagos
        {
            get => _historialPagos;
            private set => SetProperty(ref _historialPagos, value);
        }

        private PagoModel? _pagoSeleccionado;
        public PagoModel? PagoSeleccionado
        {
            get => _pagoSeleccionado;
            set
            {
                if (SetProperty(ref _pagoSeleccionado, value))
                    NotificarComandos();
            }
        }

        private string _busquedaPago = string.Empty;
        public string BusquedaPago
        {
            get => _busquedaPago;
            set
            {
                if (SetProperty(ref _busquedaPago, value ?? string.Empty))
                    AplicarFiltroPagos();
            }
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public bool SinResultadosTratamientos => !EstaCargando
            && PacienteSeleccionado is not null
            && ListaTratamientos.Count == 0;

        public string TextoEstadoTratamientos
        {
            get
            {
                if (PacienteSeleccionado is null)
                    return "Selecciona un paciente para consultar su estado de cuenta.";

                if (_tratamientosTodos.Count == 0)
                    return "Este paciente todavía no tiene tratamientos registrados.";

                return "No hay tratamientos que coincidan con los filtros actuales.";
            }
        }

        public bool SinPagos => !EstaCargando
            && TratamientoSeleccionado is not null
            && HistorialPagos.Count == 0;

        public string TextoHistorialPagos => TratamientoSeleccionado is null
            ? "Selecciona un tratamiento para consultar sus abonos."
            : _pagosTodos.Count == 0
                ? "Este tratamiento aún no tiene abonos registrados."
                : "No hay abonos que coincidan con la búsqueda.";

        public AsyncRelayCommand NuevoAbonoCommand { get; }
        public RelayCommand VerDetalleCommand { get; }
        public RelayCommand ImprimirReciboCommand { get; }
        public RelayCommand ImprimirReciboGlobalCommand { get; }
        public AsyncRelayCommand RecargarCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }

        public EstadoCuentaViewModel(Action<object> cambiarVista)
            : this(
                cambiarVista,
                new PacienteRepository(),
                new TratamientoRepository(),
                new PagoRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public EstadoCuentaViewModel(
            Action<object> cambiarVista,
            PacienteRepository pacienteRepository,
            TratamientoRepository tratamientoRepository,
            PagoRepository pagoRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _ = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _tratamientoRepository = tratamientoRepository ?? throw new ArgumentNullException(nameof(tratamientoRepository));
            _pagoRepository = pagoRepository ?? throw new ArgumentNullException(nameof(pagoRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Estado de Cuenta y Pagos";

            NuevoAbonoCommand = new AsyncRelayCommand(
                _ => AbrirNuevoAbonoAsync(),
                _ => PuedeRegistrarAbono);

            VerDetalleCommand = new RelayCommand(
                VerDetalleAbono,
                parametro => ObtenerPago(parametro) is not null || PagoSeleccionado is not null);

            ImprimirReciboCommand = new RelayCommand(
                _ => ImprimirTicketTratamiento(),
                _ => PacienteSeleccionado is not null && TratamientoSeleccionado is not null);

            ImprimirReciboGlobalCommand = new RelayCommand(
                _ => ImprimirEstadoGlobal(),
                _ => PacienteSeleccionado is not null && _tratamientosTodos.Count > 0);

            RecargarCommand = new AsyncRelayCommand(
                _ => RecargarAsync(),
                _ => PacienteSeleccionado is not null && !EstaCargando);

            LimpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());

            _ = InicializarSeguroAsync();
        }

        private async Task InicializarSeguroAsync()
        {
            try
            {
                await InicializarAsync();
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                _pacientesTodos = new List<PacienteModel>();
                ListaPacientes = new ObservableCollection<PacienteModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible inicializar el módulo de pagos.");
                NotificarEstadoVacio();
                NotificarComandos();
            }
        }

        private async Task InicializarAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;
            NotificarEstadoVacio();

            try
            {
                var pacientes = await _pacienteRepository.ObtenerTodosAsync();
                _pacientesTodos = pacientes.ToList();
                AplicarFiltroPacientes();
            }
            catch (Exception ex)
            {
                _pacientesTodos = new List<PacienteModel>();
                ListaPacientes = new ObservableCollection<PacienteModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los pacientes para el módulo de pagos.");
            }
            finally
            {
                EstaCargando = false;
                NotificarEstadoVacio();
                NotificarComandos();
            }
        }

        private async Task CargarTratamientosSeguroAsync(int idPaciente, int? idTratamientoPreferido = null)
        {
            try
            {
                await CargarTratamientosPacienteAsync(idPaciente, idTratamientoPreferido);
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar el estado de cuenta del paciente.");
                NotificarEstadoVacio();
                NotificarComandos();
            }
        }

        private async Task CargarTratamientosPacienteAsync(int idPaciente, int? idTratamientoPreferido = null)
        {
            int versionActual = Interlocked.Increment(ref _versionCargaTratamientos);
            Interlocked.Increment(ref _versionCargaPagos);
            TratamientoPacienteModel? tratamientoASeleccionar = null;

            MensajeError = string.Empty;
            EstaCargando = true;
            TratamientoSeleccionado = null;
            _pagosTodos = new List<PagoModel>();
            HistorialPagos = new ObservableCollection<PagoModel>();
            NotificarEstadoVacio();
            NotificarComandos();

            try
            {
                var tratamientosTask = _tratamientoRepository.ObtenerPorPacienteAsync(idPaciente);
                var totalesTask = _pagoRepository.ObtenerTotalesPagadosPorPacienteAsync(idPaciente);
                await Task.WhenAll(tratamientosTask, totalesTask);

                if (versionActual != Volatile.Read(ref _versionCargaTratamientos))
                    return;

                _tratamientosTodos = (await tratamientosTask).ToList();
                _totalesPagadosPorTratamiento = await totalesTask;
                AplicarFiltroTratamientos();
                ActualizarResumenPaciente();

                if (idTratamientoPreferido.HasValue)
                {
                    tratamientoASeleccionar = _tratamientosTodos.FirstOrDefault(
                        x => x.Id == idTratamientoPreferido.Value);
                }

                tratamientoASeleccionar ??= _tratamientosTodos.FirstOrDefault(
                    x => PuedeRecibirAbonos(x) && ObtenerSaldo(x) > 0m);

                tratamientoASeleccionar ??= _tratamientosTodos.FirstOrDefault(
                    x => PuedeRecibirAbonos(x));

                tratamientoASeleccionar ??= _tratamientosTodos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                if (versionActual != Volatile.Read(ref _versionCargaTratamientos))
                    return;

                _tratamientosTodos = new List<TratamientoPacienteModel>();
                _totalesPagadosPorTratamiento = new Dictionary<int, decimal>();
                ListaTratamientos = new ObservableCollection<TratamientoPacienteModel>();
                ActualizarResumenPaciente();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar el estado de cuenta del paciente.");
            }
            finally
            {
                if (versionActual == Volatile.Read(ref _versionCargaTratamientos))
                {
                    EstaCargando = false;
                    NotificarEstadoVacio();
                    NotificarComandos();
                }
            }

            if (versionActual == Volatile.Read(ref _versionCargaTratamientos)
                && PacienteSeleccionado?.IdPaciente == idPaciente)
            {
                TratamientoSeleccionado = tratamientoASeleccionar;
            }
        }

        private async Task CargarPagosSeguroAsync(TratamientoPacienteModel tratamiento)
        {
            try
            {
                await CargarEstadoCuentaTratamientoAsync(tratamiento);
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar los abonos del tratamiento.");
                NotificarEstadoVacio();
                NotificarComandos();
            }
        }

        private async Task CargarEstadoCuentaTratamientoAsync(TratamientoPacienteModel tratamiento)
        {
            int versionActual = Interlocked.Increment(ref _versionCargaPagos);

            MensajeError = string.Empty;
            EstaCargando = true;
            NotificarEstadoVacio();
            NotificarComandos();

            try
            {
                var pagos = (await _pagoRepository.ListarPagosAsync(tratamiento.Id)).ToList();

                if (versionActual != Volatile.Read(ref _versionCargaPagos)
                    || TratamientoSeleccionado?.Id != tratamiento.Id)
                    return;

                _pagosTodos = pagos;
                AplicarFiltroPagos();
                PagoSeleccionado = null;
                CostoTotal = tratamiento.CostoTotal;
                TotalAbonado = pagos.Sum(x => x.Monto);
                EstadoTratamiento = tratamiento.Estado ?? string.Empty;
                _totalesPagadosPorTratamiento[tratamiento.Id] = TotalAbonado;
                ActualizarResumenPaciente();
            }
            catch (Exception ex)
            {
                if (versionActual != Volatile.Read(ref _versionCargaPagos))
                    return;

                _pagosTodos = new List<PagoModel>();
                HistorialPagos = new ObservableCollection<PagoModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los abonos del tratamiento.");
            }
            finally
            {
                if (versionActual == Volatile.Read(ref _versionCargaPagos))
                {
                    EstaCargando = false;
                    NotificarEstadoVacio();
                    NotificarComandos();
                }
            }
        }

        private async Task AbrirNuevoAbonoAsync()
        {
            var tratamiento = TratamientoSeleccionado;
            var paciente = PacienteSeleccionado;

            if (tratamiento is null || paciente is null)
                return;

            if (!PuedeRecibirAbonos(tratamiento))
            {
                _messageService.MostrarAdvertencia(
                    tratamiento.EstaFinalizado
                        ? "No se pueden registrar nuevos abonos en un tratamiento finalizado. El historial permanece disponible para consulta."
                        : "No se pueden registrar nuevos abonos en un tratamiento cancelado.",
                    "Abono no disponible");
                return;
            }

            if (SaldoPendiente <= 0m)
            {
                _messageService.MostrarInformacion(
                    "Este tratamiento ya está completamente pagado.",
                    "Sin saldo pendiente");
                return;
            }

            var modal = new NuevoPagoWindow(
                tratamiento.Id,
                tratamiento.NombreTratamiento ?? "Tratamiento dental",
                SaldoPendiente);

            if (modal.ShowDialog() == true && modal.PagoRealizado)
            {
                await CargarTratamientosSeguroAsync(
                    paciente.IdPaciente,
                    tratamiento.Id);
            }
        }

        private async Task RecargarAsync()
        {
            var paciente = PacienteSeleccionado;
            if (paciente is null)
                return;

            int? idTratamiento = TratamientoSeleccionado?.Id;
            await CargarTratamientosSeguroAsync(paciente.IdPaciente, idTratamiento);
        }

        private void VerDetalleAbono(object? parameter)
        {
            var pago = ObtenerPago(parameter) ?? PagoSeleccionado;
            if (pago is null)
                return;

            string detalle =
                $"Pago #{pago.IdPago}\n" +
                $"Monto: {pago.Monto:C}\n" +
                $"Fecha: {pago.FechaPago:dd/MM/yyyy hh:mm tt}\n" +
                $"Método: {ValorOAlternativa(pago.MetodoPago, "No especificado")}\n\n" +
                $"Observación:\n{ValorOAlternativa(pago.Observacion, "Sin observaciones registradas.")}";

            _messageService.MostrarInformacion(detalle, "Detalle del abono");
        }

        private void ImprimirTicketTratamiento()
        {
            if (PacienteSeleccionado is null || TratamientoSeleccionado is null)
                return;

            try
            {
                var ventanaPrevia = new VistaPreviaReciboWindow(
                    PacienteSeleccionado.NombreCompleto,
                    TratamientoSeleccionado.NombreTratamiento ?? "Tratamiento",
                    CostoTotal,
                    new ObservableCollection<PagoModel>(_pagosTodos));

                ventanaPrevia.ShowDialog();
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(ex, "No fue posible generar la vista previa del recibo.");
            }
        }

        private void ImprimirEstadoGlobal()
        {
            if (PacienteSeleccionado is null || _tratamientosTodos.Count == 0)
                return;

            try
            {
                var ventanaGlobal = new VistaPreviaEstadoGlobalWindow(
                    PacienteSeleccionado.NombreCompleto,
                    new ObservableCollection<TratamientoPacienteModel>(_tratamientosTodos),
                    new Dictionary<int, decimal>(_totalesPagadosPorTratamiento));

                ventanaGlobal.ShowDialog();
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(ex, "No fue posible generar el estado de cuenta global.");
            }
        }

        private void AplicarFiltroPacientes()
        {
            string termino = BusquedaPaciente.Trim();
            IEnumerable<PacienteModel> consulta = _pacientesTodos;

            if (!string.IsNullOrWhiteSpace(termino))
            {
                consulta = consulta.Where(p =>
                    Contiene(p.NombreCompleto, termino)
                    || Contiene(p.DUI, termino)
                    || Contiene(p.Telefono, termino));
            }

            ListaPacientes = new ObservableCollection<PacienteModel>(consulta);
        }

        private void AplicarFiltroTratamientos()
        {
            string termino = BusquedaTratamiento.Trim();
            string estado = EstadoFiltroSeleccionado;

            IEnumerable<TratamientoPacienteModel> consulta = _tratamientosTodos;

            if (estado != TodosLosEstados)
            {
                consulta = consulta.Where(x =>
                    string.Equals(x.Estado, estado, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(termino))
            {
                consulta = consulta.Where(x =>
                    Contiene(x.NombreTratamiento, termino)
                    || Contiene(x.NombreDoctor, termino)
                    || Contiene(x.Observaciones, termino));
            }

            ListaTratamientos = new ObservableCollection<TratamientoPacienteModel>(consulta);

            if (TratamientoSeleccionado is not null
                && !ListaTratamientos.Any(x => x.Id == TratamientoSeleccionado.Id))
            {
                TratamientoSeleccionado = null;
            }

            NotificarEstadoVacio();
        }

        private void AplicarFiltroPagos()
        {
            string termino = BusquedaPago.Trim();
            IEnumerable<PagoModel> consulta = _pagosTodos;

            if (!string.IsNullOrWhiteSpace(termino))
            {
                consulta = consulta.Where(p =>
                    Contiene(p.MetodoPago, termino)
                    || Contiene(p.Observacion, termino)
                    || p.FechaPago.ToString("dd/MM/yyyy").Contains(termino, StringComparison.OrdinalIgnoreCase)
                    || p.Monto.ToString("0.00").Contains(termino, StringComparison.OrdinalIgnoreCase));
            }

            HistorialPagos = new ObservableCollection<PagoModel>(consulta);

            if (PagoSeleccionado is not null
                && !HistorialPagos.Any(x => x.IdPago == PagoSeleccionado.IdPago))
            {
                PagoSeleccionado = null;
            }

            NotificarEstadoVacio();
        }

        private void LimpiarFiltros()
        {
            BusquedaTratamiento = string.Empty;
            EstadoFiltroSeleccionado = TodosLosEstados;
            BusquedaPago = string.Empty;
        }

        private decimal ObtenerSaldo(TratamientoPacienteModel tratamiento)
        {
            decimal pagado = _totalesPagadosPorTratamiento.TryGetValue(tratamiento.Id, out decimal total)
                ? total
                : 0m;

            return Math.Max(0m, tratamiento.CostoTotal - pagado);
        }

        private void ActualizarResumenPaciente()
        {
            TotalCargosPaciente = _tratamientosTodos.Sum(x => x.CostoTotal);
            TotalPagadoPaciente = _totalesPagadosPorTratamiento.Values.Sum();
            NotificarResumenPaciente();
        }

        private void LimpiarEstadoCuentaCompleto()
        {
            _tratamientosTodos = new List<TratamientoPacienteModel>();
            _pagosTodos = new List<PagoModel>();
            _totalesPagadosPorTratamiento = new Dictionary<int, decimal>();
            ListaTratamientos = new ObservableCollection<TratamientoPacienteModel>();
            TratamientoSeleccionado = null;
            TotalCargosPaciente = 0m;
            TotalPagadoPaciente = 0m;
            LimpiarEstadoTratamiento();
            MensajeError = string.Empty;
        }

        private void LimpiarEstadoTratamiento()
        {
            CostoTotal = 0m;
            TotalAbonado = 0m;
            EstadoTratamiento = string.Empty;
            _pagosTodos = new List<PagoModel>();
            HistorialPagos = new ObservableCollection<PagoModel>();
            PagoSeleccionado = null;
            OnPropertyChanged(nameof(PuedeRegistrarAbono));
            OnPropertyChanged(nameof(MensajeAccionPago));
        }

        private void NotificarResumenTratamiento()
        {
            OnPropertyChanged(nameof(SaldoPendiente));
            OnPropertyChanged(nameof(PorcentajePagado));
            OnPropertyChanged(nameof(PuedeRegistrarAbono));
            OnPropertyChanged(nameof(MensajeAccionPago));
            NuevoAbonoCommand?.NotificarCanExecuteChanged();
        }

        private void NotificarResumenPaciente()
        {
            OnPropertyChanged(nameof(SaldoTotalPaciente));
            OnPropertyChanged(nameof(TextoEstadoTratamientos));
            OnPropertyChanged(nameof(SinResultadosTratamientos));
            ImprimirReciboGlobalCommand?.NotificarCanExecuteChanged();
        }

        private void NotificarEstadoVacio()
        {
            OnPropertyChanged(nameof(SinResultadosTratamientos));
            OnPropertyChanged(nameof(TextoEstadoTratamientos));
            OnPropertyChanged(nameof(SinPagos));
            OnPropertyChanged(nameof(TextoHistorialPagos));
        }

        private void NotificarComandos()
        {
            OnPropertyChanged(nameof(PuedeRegistrarAbono));
            NuevoAbonoCommand?.NotificarCanExecuteChanged();
            VerDetalleCommand?.NotificarCanExecuteChanged();
            ImprimirReciboCommand?.NotificarCanExecuteChanged();
            ImprimirReciboGlobalCommand?.NotificarCanExecuteChanged();
            RecargarCommand?.NotificarCanExecuteChanged();
        }

        private static bool PuedeRecibirAbonos(TratamientoPacienteModel tratamiento)
        {
            return tratamiento.EstaPendiente || tratamiento.EstaEnProgreso;
        }

        private static PagoModel? ObtenerPago(object? parameter) => parameter as PagoModel;

        private static bool Contiene(string? texto, string termino)
        {
            return !string.IsNullOrWhiteSpace(texto)
                && texto.Contains(termino, StringComparison.OrdinalIgnoreCase);
        }

        private static string ValorOAlternativa(string? valor, string alternativa)
        {
            return string.IsNullOrWhiteSpace(valor) ? alternativa : valor.Trim();
        }
    }
}
