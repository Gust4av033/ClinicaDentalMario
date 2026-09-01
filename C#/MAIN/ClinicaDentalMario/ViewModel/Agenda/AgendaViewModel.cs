using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda;
using System.Collections.ObjectModel;
using System.Windows;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class AgendaViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly CitaRepository _citaRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private ObservableCollection<AgendaCitaModel> _citasDelDia = new();
        public ObservableCollection<AgendaCitaModel> CitasDelDia
        {
            get => _citasDelDia;
            private set
            {
                if (SetProperty(ref _citasDelDia, value))
                {
                    OnPropertyChanged(nameof(SinCitas));
                }
            }
        }

        public bool SinCitas => !EstaCargando && CitasDelDia.Count == 0;

        private DateTime _fechaSeleccionada = DateTime.Today;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (SetProperty(ref _fechaSeleccionada, value.Date))
                {
                    CerrarDetalle();
                    _ = CargarCitasDelDiaAsync();
                }
            }
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
        public AsyncRelayCommand CancelarCitaCommand { get; }
        public AsyncRelayCommand FinalizarCitaCommand { get; }
        public AsyncRelayCommand NoAsistioCommand { get; }
        public RelayCommand CerrarDetalleCommand { get; }
        public AsyncRelayCommand RecargarCommand { get; }

        public AgendaViewModel(Action<object> cambiarVista)
            : this(
                cambiarVista,
                new CitaRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public AgendaViewModel(
            Action<object> cambiarVista,
            CitaRepository citaRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _citaRepository = citaRepository ?? throw new ArgumentNullException(nameof(citaRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Agenda de Citas";
            NuevaCitaCommand = new RelayCommand(_ => AbrirNuevaCita());
            EditarCitaCommand = new RelayCommand(_ => AbrirEditarCita(), _ => PuedeReprogramarOCancelar());
            CancelarCitaCommand = new AsyncRelayCommand(_ => CancelarCitaAsync(), _ => PuedeReprogramarOCancelar());
            FinalizarCitaCommand = new AsyncRelayCommand(_ => FinalizarCitaAsync(), _ => PuedeMarcarAtendida());
            NoAsistioCommand = new AsyncRelayCommand(_ => MarcarNoAsistioAsync(), _ => PuedeMarcarNoAsistio());
            CerrarDetalleCommand = new RelayCommand(_ => CerrarDetalle());
            RecargarCommand = new AsyncRelayCommand(_ => CargarCitasDelDiaAsync());

            _ = CargarCitasDelDiaAsync();
        }

        private async Task CargarCitasDelDiaAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;
            OnPropertyChanged(nameof(SinCitas));

            try
            {
                var citas = await _citaRepository.ObtenerCitasPorFechaAsync(FechaSeleccionada);
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

        private void AbrirNuevaCita()
        {
            var vista = new NuevaCitaView
            {
                DataContext = new NuevaCitaViewModel(_cambiarVista)
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

        private async Task CancelarCitaAsync()
        {
            if (!PuedeReprogramarOCancelar())
            {
                return;
            }

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Deseas cancelar la cita de {cita.Paciente}?",
                    "Cancelar cita"))
            {
                return;
            }

            await CambiarEstadoSeleccionadaAsync(
                "Cancelada",
                "La cita fue cancelada correctamente.",
                "Cita cancelada");
        }

        private async Task FinalizarCitaAsync()
        {
            if (!PuedeMarcarAtendida())
            {
                return;
            }

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Confirmas que {cita.Paciente} fue atendido?",
                    "Marcar como atendida"))
            {
                return;
            }

            await CambiarEstadoSeleccionadaAsync(
                "Atendida",
                "La cita fue marcada como atendida.",
                "Cita atendida");
        }

        private async Task MarcarNoAsistioAsync()
        {
            if (!PuedeMarcarNoAsistio())
            {
                return;
            }

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Confirmas que {cita.Paciente} no asistió a la cita?",
                    "Marcar inasistencia"))
            {
                return;
            }

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
                await CargarCitasDelDiaAsync();
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar el estado de la cita.");
            }
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
                   CitaSeleccionada.FechaHora < DateTime.Now;
        }

        private void NotificarComandosSeleccion()
        {
            EditarCitaCommand.NotificarCanExecuteChanged();
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
