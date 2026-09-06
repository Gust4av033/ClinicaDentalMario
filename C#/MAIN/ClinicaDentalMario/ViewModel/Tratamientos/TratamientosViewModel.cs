using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Tratamientos;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class TratamientosViewModel : ViewModelBase
    {
        private const string TodosLosEstados = "Todos";

        private readonly Action<object> _cambiarVista;
        private readonly PacienteRepository _pacienteRepository;
        private readonly TratamientoRepository _tratamientoRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly int? _idPacienteInicial;

        private int _versionCargaTratamientos;

        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            private set
            {
                if (SetProperty(ref _listaPacientes, value))
                    AplicarFiltroPacientes();
            }
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set
            {
                if (!SetProperty(ref _pacienteSeleccionado, value))
                    return;

                // Invalida cualquier consulta anterior que todavía esté esperando respuesta.
                Interlocked.Increment(ref _versionCargaTratamientos);

                TratamientoSeleccionado = null;
                NotificarComandos();
                OnPropertyChanged(nameof(TextoEstadoVacio));

                if (value is null)
                {
                    TratamientosDelPaciente = new ObservableCollection<TratamientoPacienteModel>();
                    return;
                }

                _ = CargarTratamientosAsync(value.IdPaciente);
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

        private ObservableCollection<TratamientoPacienteModel> _tratamientosDelPaciente = new();
        public ObservableCollection<TratamientoPacienteModel> TratamientosDelPaciente
        {
            get => _tratamientosDelPaciente;
            private set
            {
                if (!SetProperty(ref _tratamientosDelPaciente, value))
                    return;

                AplicarFiltroTratamientos();
                NotificarResumen();
            }
        }

        private TratamientoPacienteModel? _tratamientoSeleccionado;
        public TratamientoPacienteModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (SetProperty(ref _tratamientoSeleccionado, value))
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

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public int TotalTratamientos => TratamientosDelPaciente.Count;

        public int TratamientosActivos => TratamientosDelPaciente.Count(
            x => x.EstaPendiente || x.EstaEnProgreso);

        public int TratamientosFinalizados => TratamientosDelPaciente.Count(x => x.EstaFinalizado);

        public decimal CostoPlanificado => TratamientosDelPaciente
            .Where(x => !x.EstaCancelado)
            .Sum(x => x.CostoTotal);

        public bool SinResultadosTratamientos
        {
            get
            {
                if (EstaCargando || PacienteSeleccionado is null)
                    return false;

                var vista = CollectionViewSource.GetDefaultView(TratamientosDelPaciente);
                return vista.IsEmpty;
            }
        }

        public string TextoEstadoVacio
        {
            get
            {
                if (PacienteSeleccionado is null)
                    return "Selecciona un paciente para consultar su plan de tratamiento.";

                if (TratamientosDelPaciente.Count == 0)
                    return "Este paciente todavía no tiene tratamientos asignados.";

                return "No hay tratamientos que coincidan con los filtros actuales.";
            }
        }

        public RelayCommand NuevoTratamientoCommand { get; }
        public RelayCommand VerDetalleCommand { get; }
        public RelayCommand EditarTratamientoCommand { get; }
        public AsyncRelayCommand IniciarTratamientoCommand { get; }
        public AsyncRelayCommand FinalizarTratamientoCommand { get; }
        public AsyncRelayCommand CancelarTratamientoCommand { get; }
        public AsyncRelayCommand RecargarCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }

        public TratamientosViewModel(Action<object> cambiarVista, int? idPacienteInicial = null)
            : this(
                cambiarVista,
                idPacienteInicial,
                new PacienteRepository(),
                new TratamientoRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public TratamientosViewModel(
            Action<object> cambiarVista,
            int? idPacienteInicial,
            PacienteRepository pacienteRepository,
            TratamientoRepository tratamientoRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _idPacienteInicial = idPacienteInicial;
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _tratamientoRepository = tratamientoRepository ?? throw new ArgumentNullException(nameof(tratamientoRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Gestión de Tratamientos";

            NuevoTratamientoCommand = new RelayCommand(
                _ => AbrirNuevoTratamiento(),
                _ => PacienteSeleccionado is not null);

            VerDetalleCommand = new RelayCommand(
                VerDetalleTratamiento,
                parametro => ObtenerTratamiento(parametro) is not null);

            EditarTratamientoCommand = new RelayCommand(
                AbrirEdicionTratamiento,
                parametro => ObtenerTratamiento(parametro)?.PuedeEditar == true);

            IniciarTratamientoCommand = new AsyncRelayCommand(
                _ => IniciarTratamientoAsync(),
                _ => TratamientoSeleccionado?.PuedeIniciar == true);

            FinalizarTratamientoCommand = new AsyncRelayCommand(
                _ => FinalizarTratamientoAsync(),
                _ => TratamientoSeleccionado?.PuedeFinalizar == true);

            CancelarTratamientoCommand = new AsyncRelayCommand(
                _ => CancelarTratamientoAsync(),
                _ => TratamientoSeleccionado?.PuedeCancelar == true);

            RecargarCommand = new AsyncRelayCommand(
                _ => RecargarAsync(),
                _ => PacienteSeleccionado is not null);

            LimpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;
            NotificarEstadoVacio();

            PacienteModel? pacienteInicial = null;

            try
            {
                var pacientes = await _pacienteRepository.ObtenerTodosAsync();
                ListaPacientes = new ObservableCollection<PacienteModel>(pacientes);

                if (_idPacienteInicial.HasValue)
                {
                    pacienteInicial = ListaPacientes.FirstOrDefault(
                        x => x.IdPaciente == _idPacienteInicial.Value);
                }
            }
            catch (Exception ex)
            {
                ListaPacientes = new ObservableCollection<PacienteModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los pacientes para tratamientos.");
            }
            finally
            {
                EstaCargando = false;
                NotificarEstadoVacio();
            }

            if (pacienteInicial is not null)
                PacienteSeleccionado = pacienteInicial;
        }

        private async Task CargarTratamientosAsync(int idPaciente)
        {
            int versionActual = Interlocked.Increment(ref _versionCargaTratamientos);

            MensajeError = string.Empty;
            EstaCargando = true;
            NotificarEstadoVacio();

            try
            {
                var lista = await _tratamientoRepository.ObtenerPorPacienteAsync(idPaciente);

                if (versionActual != Volatile.Read(ref _versionCargaTratamientos))
                    return;

                TratamientosDelPaciente = new ObservableCollection<TratamientoPacienteModel>(lista);
                TratamientoSeleccionado = null;
            }
            catch (Exception ex)
            {
                if (versionActual != Volatile.Read(ref _versionCargaTratamientos))
                    return;

                TratamientosDelPaciente = new ObservableCollection<TratamientoPacienteModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los tratamientos del paciente.");
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
        }

        private async Task RecargarAsync()
        {
            if (PacienteSeleccionado is null)
                return;

            await CargarTratamientosAsync(PacienteSeleccionado.IdPaciente);
        }

        private void AplicarFiltroPacientes()
        {
            var vista = CollectionViewSource.GetDefaultView(ListaPacientes);
            string termino = BusquedaPaciente.Trim();

            if (string.IsNullOrWhiteSpace(termino))
            {
                vista.Filter = null;
            }
            else
            {
                vista.Filter = item =>
                {
                    if (item is not PacienteModel paciente)
                        return false;

                    return Contiene(paciente.NombreCompleto, termino)
                        || Contiene(paciente.DUI, termino)
                        || Contiene(paciente.Telefono, termino);
                };
            }

            vista.Refresh();
        }

        private void AplicarFiltroTratamientos()
        {
            var vista = CollectionViewSource.GetDefaultView(TratamientosDelPaciente);
            string termino = BusquedaTratamiento.Trim();
            string estado = EstadoFiltroSeleccionado;

            vista.Filter = item =>
            {
                if (item is not TratamientoPacienteModel tratamiento)
                    return false;

                bool coincideEstado = estado == TodosLosEstados
                    || string.Equals(tratamiento.Estado, estado, StringComparison.OrdinalIgnoreCase);

                if (!coincideEstado)
                    return false;

                if (string.IsNullOrWhiteSpace(termino))
                    return true;

                return Contiene(tratamiento.NombreTratamiento, termino)
                    || Contiene(tratamiento.NombreDoctor, termino)
                    || Contiene(tratamiento.Observaciones, termino);
            };

            vista.Refresh();
            NotificarEstadoVacio();
        }

        private void LimpiarFiltros()
        {
            BusquedaTratamiento = string.Empty;
            EstadoFiltroSeleccionado = TodosLosEstados;
        }

        private async Task IniciarTratamientoAsync()
        {
            var tratamiento = TratamientoSeleccionado;
            if (tratamiento?.PuedeIniciar != true)
                return;

            if (!_messageService.Confirmar(
                    $"¿Deseas iniciar el tratamiento '{tratamiento.NombreTratamiento}'?",
                    "Iniciar tratamiento"))
            {
                return;
            }

            await CambiarEstadoAsync(tratamiento, "En progreso", "Tratamiento iniciado correctamente.");
        }

        private async Task FinalizarTratamientoAsync()
        {
            var tratamiento = TratamientoSeleccionado;
            if (tratamiento?.PuedeFinalizar != true)
                return;

            if (!_messageService.Confirmar(
                    $"¿Deseas finalizar el tratamiento '{tratamiento.NombreTratamiento}'? Esta acción cerrará el tratamiento.",
                    "Finalizar tratamiento"))
            {
                return;
            }

            await CambiarEstadoAsync(tratamiento, "Finalizado", "Tratamiento finalizado correctamente.");
        }

        private async Task CancelarTratamientoAsync()
        {
            var tratamiento = TratamientoSeleccionado;
            if (tratamiento?.PuedeCancelar != true)
                return;

            if (!_messageService.Confirmar(
                    $"¿Deseas cancelar el tratamiento '{tratamiento.NombreTratamiento}'?",
                    "Cancelar tratamiento"))
            {
                return;
            }

            await CambiarEstadoAsync(tratamiento, "Cancelado", "Tratamiento cancelado correctamente.");
        }

        private async Task CambiarEstadoAsync(
            TratamientoPacienteModel tratamiento,
            string nuevoEstado,
            string mensajeExito)
        {
            MensajeError = string.Empty;

            try
            {
                await _tratamientoRepository.CambiarEstadoTratamientoAsync(tratamiento.Id, nuevoEstado);
                _messageService.MostrarExito(mensajeExito);

                if (PacienteSeleccionado is not null)
                    await CargarTratamientosAsync(PacienteSeleccionado.IdPaciente);
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar el estado del tratamiento.");
            }
        }

        private void AbrirNuevoTratamiento()
        {
            if (PacienteSeleccionado is null)
                return;

            var vista = new NuevoTratamientoView
            {
                DataContext = new NuevoTratamientoViewModel(
                    PacienteSeleccionado.IdPaciente,
                    PacienteSeleccionado.NombreCompleto,
                    _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void VerDetalleTratamiento(object? parameter)
        {
            var tratamiento = ObtenerTratamiento(parameter) ?? TratamientoSeleccionado;
            if (tratamiento is null)
                return;

            string fechaInicio = tratamiento.FechaInicio?.ToString("dd/MM/yyyy HH:mm") ?? "Sin fecha";
            string fechaFin = tratamiento.FechaFin?.ToString("dd/MM/yyyy HH:mm") ?? "—";
            string doctor = string.IsNullOrWhiteSpace(tratamiento.NombreDoctor)
                ? "Sin doctor registrado"
                : tratamiento.NombreDoctor;
            string observaciones = string.IsNullOrWhiteSpace(tratamiento.Observaciones)
                ? "Sin observaciones registradas."
                : tratamiento.Observaciones;

            string detalle =
                $"Tratamiento: {tratamiento.NombreTratamiento}\n" +
                $"Doctor: {doctor}\n" +
                $"Estado: {tratamiento.Estado}\n" +
                $"Inicio: {fechaInicio}\n" +
                $"Fin: {fechaFin}\n" +
                $"Costo acordado: {tratamiento.CostoTotal:C}\n\n" +
                $"Plan clínico / observaciones:\n{observaciones}";

            _messageService.MostrarInformacion(detalle, "Detalle del tratamiento");
        }

        private void AbrirEdicionTratamiento(object? parameter)
        {
            var tratamiento = ObtenerTratamiento(parameter) ?? TratamientoSeleccionado;
            if (tratamiento is null)
                return;

            if (!tratamiento.PuedeEditar)
            {
                _messageService.MostrarAdvertencia(
                    "Los tratamientos finalizados o cancelados se conservan como historial y no pueden editarse.",
                    "Tratamiento no editable");
                return;
            }

            var vmEdicion = new EditarTratamientoViewModel(
                tratamiento.Id,
                tratamiento.NombreTratamiento ?? "Tratamiento dental",
                tratamiento.CostoTotal,
                tratamiento.Observaciones);

            var modal = new EditarTratamientoWindow
            {
                DataContext = vmEdicion
            };

            if (modal.ShowDialog() == true && PacienteSeleccionado is not null)
                _ = CargarTratamientosAsync(PacienteSeleccionado.IdPaciente);
        }

        private static TratamientoPacienteModel? ObtenerTratamiento(object? parameter)
        {
            return parameter as TratamientoPacienteModel;
        }

        private static bool Contiene(string? texto, string termino)
        {
            return !string.IsNullOrWhiteSpace(texto)
                && texto.Contains(termino, StringComparison.OrdinalIgnoreCase);
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(TotalTratamientos));
            OnPropertyChanged(nameof(TratamientosActivos));
            OnPropertyChanged(nameof(TratamientosFinalizados));
            OnPropertyChanged(nameof(CostoPlanificado));
            OnPropertyChanged(nameof(TextoEstadoVacio));
            OnPropertyChanged(nameof(SinResultadosTratamientos));
        }

        private void NotificarEstadoVacio()
        {
            OnPropertyChanged(nameof(SinResultadosTratamientos));
            OnPropertyChanged(nameof(TextoEstadoVacio));
        }

        private void NotificarComandos()
        {
            NuevoTratamientoCommand.NotificarCanExecuteChanged();
            EditarTratamientoCommand.NotificarCanExecuteChanged();
            IniciarTratamientoCommand.NotificarCanExecuteChanged();
            FinalizarTratamientoCommand.NotificarCanExecuteChanged();
            CancelarTratamientoCommand.NotificarCanExecuteChanged();
            RecargarCommand.NotificarCanExecuteChanged();
        }
    }
}
