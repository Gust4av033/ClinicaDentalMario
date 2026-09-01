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
    public class EditarCitaViewModel : ValidatableViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly CitaRepository _citaRepository;
        private readonly DoctorRepository _doctorRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly int _idCita;

        public string NombrePaciente { get; }

        private ObservableCollection<DoctorModel> _listaDoctores = new();
        public ObservableCollection<DoctorModel> ListaDoctores
        {
            get => _listaDoctores;
            private set => SetProperty(ref _listaDoctores, value);
        }

        private ObservableCollection<EstadoCitaModel> _listaEstados = new();
        public ObservableCollection<EstadoCitaModel> ListaEstados
        {
            get => _listaEstados;
            private set => SetProperty(ref _listaEstados, value);
        }

        private DoctorModel? _doctorSeleccionado;
        public DoctorModel? DoctorSeleccionado
        {
            get => _doctorSeleccionado;
            set
            {
                if (SetProperty(ref _doctorSeleccionado, value))
                {
                    ValidarDoctor();
                }
            }
        }

        private EstadoCitaModel? _estadoSeleccionado;
        public EstadoCitaModel? EstadoSeleccionado
        {
            get => _estadoSeleccionado;
            set
            {
                if (SetProperty(ref _estadoSeleccionado, value))
                {
                    ValidarEstado();
                }
            }
        }

        private DateTime _fechaSeleccionada;
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

        private string _horaSeleccionada = string.Empty;
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

        public AsyncRelayCommand ActualizarCommand { get; }
        public ICommand VolverCommand { get; }

        public EditarCitaViewModel(AgendaCitaModel cita, Action<object> cambiarVista)
            : this(
                cita,
                cambiarVista,
                new CitaRepository(),
                new DoctorRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public EditarCitaViewModel(
            AgendaCitaModel cita,
            Action<object> cambiarVista,
            CitaRepository citaRepository,
            DoctorRepository doctorRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            ArgumentNullException.ThrowIfNull(cita);

            _idCita = cita.IdCita;
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _citaRepository = citaRepository ?? throw new ArgumentNullException(nameof(citaRepository));
            _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Modificar o Reprogramar Cita";
            NombrePaciente = cita.Paciente;
            _fechaSeleccionada = cita.FechaHora.Date;
            _horaSeleccionada = cita.FechaHora.ToString("HH:mm");
            _observaciones = cita.Observaciones ?? string.Empty;

            ActualizarCommand = new AsyncRelayCommand(_ => ActualizarAsync(), _ => !EstaCargando);
            VolverCommand = new RelayCommand(_ => Volver());

            _ = CargarCatalogosAsync(cita.IdDoctor, cita.IdEstado);
        }

        private async Task CargarCatalogosAsync(int idDoctorActual, int idEstadoActual)
        {
            MensajeError = string.Empty;

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    var doctores = await _doctorRepository.ObtenerDoctoresActivosAsync();
                    var estados = await _citaRepository.ObtenerEstadosAsync();

                    ListaDoctores = new ObservableCollection<DoctorModel>(doctores);
                    ListaEstados = new ObservableCollection<EstadoCitaModel>(estados);

                    DoctorSeleccionado = ListaDoctores.FirstOrDefault(x => x.IdDoctor == idDoctorActual);
                    EstadoSeleccionado = ListaEstados.FirstOrDefault(x => x.IdEstado == idEstadoActual);
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible cargar los datos necesarios para editar la cita.");
                }
            });
        }

        private async Task ActualizarAsync()
        {
            MensajeError = string.Empty;

            if (!ValidarFormulario(out DateTime nuevaFechaHora))
            {
                MensajeError = "Revisa los campos marcados antes de actualizar.";
                return;
            }

            if (EstadoSeleccionado!.Nombre.Equals("Cancelada", StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "Para cancelar una cita usa la acción Cancelar desde la agenda.";
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    if (await _citaRepository.ExisteConflictoDoctorAsync(
                            DoctorSeleccionado!.IdDoctor,
                            nuevaFechaHora,
                            _idCita))
                    {
                        MensajeError = "El doctor ya tiene otra cita asignada exactamente a esa hora.";
                        return;
                    }

                    await _citaRepository.ActualizarCitaAsync(
                        _idCita,
                        DoctorSeleccionado.IdDoctor,
                        EstadoSeleccionado.IdEstado,
                        nuevaFechaHora,
                        Observaciones);

                    _messageService.MostrarExito(
                        "La cita fue actualizada correctamente.",
                        "Cita actualizada");

                    Volver();
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible actualizar la cita.");
                }
            });
        }

        private bool ValidarFormulario(out DateTime fechaHora)
        {
            ValidarDoctor();
            ValidarEstado();
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
                    new[] { "La nueva fecha y hora deben ser posteriores al momento actual." },
                    nameof(HoraSeleccionada));
                return false;
            }

            return true;
        }

        private void ValidarDoctor()
        {
            ValidarCampo(
                DoctorSeleccionado is null
                    ? new[] { "Debe seleccionar un doctor." }
                    : Array.Empty<string>(),
                nameof(DoctorSeleccionado));
        }

        private void ValidarEstado()
        {
            ValidarCampo(
                EstadoSeleccionado is null
                    ? new[] { "Debe seleccionar un estado." }
                    : Array.Empty<string>(),
                nameof(EstadoSeleccionado));
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

        private void Volver()
        {
            var vistaAgenda = new AgendaView
            {
                DataContext = new AgendaViewModel(_cambiarVista)
            };

            _cambiarVista(vistaAgenda);
        }
    }
}
