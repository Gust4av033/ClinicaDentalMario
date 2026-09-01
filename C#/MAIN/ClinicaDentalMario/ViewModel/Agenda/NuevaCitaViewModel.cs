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
                {
                    ValidarSeleccionPaciente();
                }
            }
        }

        private DoctorModel? _doctorSeleccionado;
        public DoctorModel? DoctorSeleccionado
        {
            get => _doctorSeleccionado;
            set
            {
                if (SetProperty(ref _doctorSeleccionado, value))
                {
                    ValidarSeleccionDoctor();
                }
            }
        }

        private DateTime _fechaSeleccionada = DateTime.Today.AddDays(1);
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (SetProperty(ref _fechaSeleccionada, value.Date))
                {
                    ValidarFecha();
                }
            }
        }

        private string _horaSeleccionada = "10:00";
        public string HoraSeleccionada
        {
            get => _horaSeleccionada;
            set
            {
                if (SetProperty(ref _horaSeleccionada, value ?? string.Empty))
                {
                    ValidarHora();
                }
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

        public NuevaCitaViewModel(Action<object> cambiarVista)
            : this(
                cambiarVista,
                new PacienteRepository(),
                new DoctorRepository(),
                new CitaRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public NuevaCitaViewModel(
            Action<object> cambiarVista,
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

            Titulo = "Agendar Nueva Cita";
            GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync(), _ => !EstaCargando);
            CancelarCommand = new RelayCommand(_ => VolverAAgenda());

            _ = CargarListasAsync();
        }

        private async Task CargarListasAsync()
        {
            MensajeError = string.Empty;

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    var pacientes = await _pacienteRepository.ObtenerTodosAsync();
                    var doctores = await _doctorRepository.ObtenerDoctoresActivosAsync();

                    ListaPacientes = new ObservableCollection<PacienteModel>(pacientes);
                    ListaDoctores = new ObservableCollection<DoctorModel>(doctores);
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible cargar pacientes y doctores.");
                }
            });
        }

        private async Task GuardarAsync()
        {
            MensajeError = string.Empty;

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
                            fechaHora))
                    {
                        MensajeError = "El doctor ya tiene una cita asignada exactamente a esa hora.";
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
                        Observaciones = string.IsNullOrWhiteSpace(Observaciones)
                            ? null
                            : Observaciones.Trim()
                    };

                    await _citaRepository.InsertarAsync(cita);

                    _messageService.MostrarExito(
                        $"Cita agendada para el {fechaHora:dd/MM/yyyy} a las {fechaHora:HH:mm}.",
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
            ValidarCampo(
                ValidationRules.LongitudMaxima(Observaciones, 500, "Las observaciones"),
                nameof(Observaciones));

            fechaHora = default;
            if (HasErrors || !TryObtenerHora(out TimeSpan hora))
            {
                return false;
            }

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
                DataContext = new AgendaViewModel(_cambiarVista)
            };

            _cambiarVista(vistaAgenda);
        }
    }
}
