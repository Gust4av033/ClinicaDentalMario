using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

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

        public ICommand NuevaCitaCommand { get; }
        public ICommand EditarCitaCommand { get; }
        public AsyncRelayCommand CancelarCitaCommand { get; }
        public AsyncRelayCommand FinalizarCitaCommand { get; }
        public ICommand CerrarDetalleCommand { get; }
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
            EditarCitaCommand = new RelayCommand(_ => AbrirEditarCita(), _ => PuedeModificarSeleccionada());
            CancelarCitaCommand = new AsyncRelayCommand(_ => CancelarCitaAsync(), _ => PuedeModificarSeleccionada());
            FinalizarCitaCommand = new AsyncRelayCommand(_ => FinalizarCitaAsync(), _ => PuedeModificarSeleccionada());
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
            if (!PuedeModificarSeleccionada())
            {
                _messageService.MostrarAdvertencia(
                    "La cita seleccionada ya está cerrada y no puede reprogramarse.",
                    "Cita cerrada");
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
            if (!PuedeModificarSeleccionada())
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

            try
            {
                await _citaRepository.CancelarCitaAsync(cita.IdCita);
                _messageService.MostrarExito("La cita fue cancelada correctamente.", "Cita cancelada");
                CerrarDetalle();
                await CargarCitasDelDiaAsync();
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cancelar la cita.");
            }
        }

        private async Task FinalizarCitaAsync()
        {
            if (!PuedeModificarSeleccionada())
            {
                return;
            }

            AgendaCitaModel cita = CitaSeleccionada!;
            if (!_messageService.Confirmar(
                    $"¿Confirmas que {cita.Paciente} ya fue atendido?",
                    "Marcar como atendida"))
            {
                return;
            }

            try
            {
                await _citaRepository.CambiarEstadoCitaAsync(cita.IdCita, "Atendida");
                _messageService.MostrarExito("La cita fue marcada como atendida.", "Cita atendida");
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

        private bool PuedeModificarSeleccionada()
        {
            return CitaSeleccionada is not null && !CitaSeleccionada.EstaCerrada;
        }

        private void CerrarDetalle()
        {
            CitaSeleccionada = null;
        }
    }
}
