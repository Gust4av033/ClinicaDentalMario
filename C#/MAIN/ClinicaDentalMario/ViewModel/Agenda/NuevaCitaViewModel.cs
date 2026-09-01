using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.Validators;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class NuevaCitaViewModel : ValidatableViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly PacienteRepository _pacienteRepository;
        private readonly DoctorRepository _doctorRepository;
        private readonly CitaRepository _citaRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private bool _listasCargadas;

        public IReadOnlyList<int> DuracionesDisponibles { get; } = new[] { 15, 30, 45, 60, 90 };

        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            private set => SetProperty(ref _listaPacientes, value);
        }

        private ObservableCollection<DoctorModel> _listaDoctores = new();
        public ObservableCollection<DoctorModel> ListaDoctores
        {
            get => _listaDoctores;
            private set => SetProperty(ref _listaDoctores, value);
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set
            {
                if (SetProperty(ref _pacienteSeleccionado, value))
                    ValidarSeleccionPaciente();
            }
        }

        private DoctorModel? _doctorSeleccionado;
        public DoctorModel? DoctorSeleccionado
        {
            get => _doctorSeleccionado;
            set
            {
                if (SetProperty(ref _doctorSeleccionado, value))
                    ValidarSeleccionDoctor();
            }
        }

        private DateTime _fechaSeleccionada;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (SetProperty(ref _fechaSeleccionada, value.Date))
                    ValidarFecha();
            }
        }

        private string _horaSeleccionada = "10:00";
        public string HoraSeleccionada
        {
            get => _horaSeleccionada;
            set
            {
                if (SetProperty(ref _horaSeleccionada, value ?? string.Empty))
                    ValidarHora();
            }
        }

        private int _duracionMinutos = 30;
        public int DuracionMinutos
        {
            get => _duracionMinutos;
            set
            {
                if (SetProperty(ref _duracionMinutos, value))
                    ValidarDuracion();
            }
        }

        private string _observaciones = string.Empty;
        public string Observaciones
        {
            get => _observaciones;
            set
            {
                if (SetProperty(ref _observaciones, value ?? string.Empty))
                {
                    ValidarCampo(
                        ValidationRules.LongitudMaxima(_observaciones, 500, "Las observaciones"),
                        nameof(Observaciones));
                }
            }
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevaCitaViewModel(Action<object> cambiarVista, DateTime? fechaInicial = null)
            : this(
                cambiarVista,
                fechaInicial,
                new PacienteRepository(),
                new DoctorRepository(),
                new CitaRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public NuevaCitaViewModel(
            Action<object> cambiarVista,
            DateTime? fechaInicial,
            PacienteRepository pacienteRepository,
            DoctorRepository doctorRepository,
            CitaRepository citaRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
            _citaRepository = citaRepository ?? throw new ArgumentNullException(nameof(citaRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            _fechaSeleccionada = fechaInicial?.Date ?? DateTime.Today.AddDays(1);
            if (_fechaSeleccionada < DateTime.Today)
                _fechaSeleccionada = DateTime.Today;

            Titulo = "Agendar Nueva Cita";
            GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync(), _ => !EstaCargando && _listasCargadas);
            CancelarCommand = new RelayCommand(_ => VolverAAgenda());

            _ = CargarListasAsync();
        }

        private async Task CargarListasAsync()
        {
            MensajeError = string.Empty;
            _listasCargadas = false;
            GuardarCommand.NotificarCanExecuteChanged();

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    var pacientes = await _pacienteRepository.ObtenerTodosAsync();
                    var doctores = await _doctorRepository.ObtenerDoctoresActivosAsync();

                    ListaPacientes = new ObservableCollection<PacienteModel>(pacientes);
                    ListaDoctores = new ObservableCollection<DoctorModel>(doctores);
                    _listasCargadas = true;
                }
                catch (Exception ex)
                {
                    _listasCargadas = false;
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible cargar pacientes y doctores.");
                }
            });

            GuardarCommand.NotificarCanExecuteChanged();
        }

        private async Task GuardarAsync()
        {
            MensajeError = string.Empty;

            if (!_listasCargadas)
            {
                MensajeError = "La información de pacientes y doctores todavía no está disponible.";
                return;
            }

            if (!ValidarFormulario(out DateTime fechaHora))
            {
                MensajeError = "Revisa los campos marcados antes de agendar.";
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    if (await _citaRepository.ExisteConflictoDoctorAsync(
                            DoctorSeleccionado!.IdDoctor,
                            fechaHora,
                            DuracionMinutos))
                    {
                        MensajeError = $"El doctor ya tiene otra cita que se cruza con el intervalo {fechaHora:HH:mm} - {fechaHora.AddMinutes(DuracionMinutos):HH:mm}.";
                        return;
                    }

                    int? idPendiente = await _citaRepository.ObtenerIdEstadoAsync("Pendiente");
                    if (!idPendiente.HasValue)
                    {
                        MensajeError = "No se encontró el estado 'Pendiente' en el catálogo de citas.";
                        return;
                    }

                    var cita = new CitaModel
                    {
                        IdPaciente = PacienteSeleccionado!.IdPaciente,
                        IdDoctor = DoctorSeleccionado.IdDoctor,
                        IdEstado = idPendiente.Value,
                        FechaHora = fechaHora,
                        DuracionMinutos = DuracionMinutos,
                        Observaciones = string.IsNullOrWhiteSpace(Observaciones)
                            ? null
                            : Observaciones.Trim()
                    };

                    await _citaRepository.InsertarAsync(cita);

                    _messageService.MostrarExito(
                        $"Cita agendada para el {fechaHora:dd/MM/yyyy}, de {fechaHora:HH:mm} a {fechaHora.AddMinutes(DuracionMinutos):HH:mm}.",
                        "Cita registrada");

                    VolverAAgenda();
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible agendar la cita.");
                }
            });
        }

        private bool ValidarFormulario(out DateTime fechaHora)
        {
            ValidarSeleccionPaciente();
            ValidarSeleccionDoctor();
            ValidarFecha();
            ValidarHora();
            ValidarDuracion();
            ValidarCampo(
                ValidationRules.LongitudMaxima(Observaciones, 500, "Las observaciones"),
                nameof(Observaciones));

            fechaHora = default;
            if (HasErrors || !TryObtenerHora(out TimeSpan hora))
                return false;

            fechaHora = FechaSeleccionada.Date.Add(hora);
            if (fechaHora <= DateTime.Now)
            {
                ValidarCampo(
                    new[] { "La fecha y hora de la cita deben ser posteriores al momento actual." },
                    nameof(HoraSeleccionada));
                return false;
            }

            return true;
        }

        private void ValidarSeleccionPaciente()
        {
            ValidarCampo(
                PacienteSeleccionado is null
                    ? new[] { "Debe seleccionar un paciente." }
                    : Array.Empty<string>(),
                nameof(PacienteSeleccionado));
        }

        private void ValidarSeleccionDoctor()
        {
            ValidarCampo(
                DoctorSeleccionado is null
                    ? new[] { "Debe seleccionar un doctor." }
                    : Array.Empty<string>(),
                nameof(DoctorSeleccionado));
        }

        private void ValidarFecha()
        {
            ValidarCampo(
                ValidationRules.FechaNoPasada(FechaSeleccionada, "La fecha de la cita"),
                nameof(FechaSeleccionada));
        }

        private void ValidarHora()
        {
            IEnumerable<string> errores = string.IsNullOrWhiteSpace(HoraSeleccionada)
                ? ValidationRules.Requerido(HoraSeleccionada, "La hora")
                : ValidationRules.Hora(HoraSeleccionada);

            ValidarCampo(errores, nameof(HoraSeleccionada));
        }

        private void ValidarDuracion()
        {
            ValidarCampo(
                DuracionesDisponibles.Contains(DuracionMinutos)
                    ? Array.Empty<string>()
                    : new[] { "Selecciona una duración válida para la cita." },
                nameof(DuracionMinutos));
        }

        private bool TryObtenerHora(out TimeSpan hora)
        {
            return TimeSpan.TryParseExact(
                HoraSeleccionada.Trim(),
                new[] { @"hh\:mm", @"h\:mm" },
                CultureInfo.InvariantCulture,
                out hora);
        }

        private void VolverAAgenda()
        {
            var vistaAgenda = new AgendaView
            {
                DataContext = new AgendaViewModel(_cambiarVista, FechaSeleccionada)
            };

            _cambiarVista(vistaAgenda);
        }
    }
}
