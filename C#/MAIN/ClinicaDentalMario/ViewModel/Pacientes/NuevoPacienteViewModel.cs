using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class NuevoPacienteViewModel : PacienteFormViewModelBase
    {
        private readonly PacienteRepository _pacienteRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly Action<object> _cambiarVista;

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoPacienteViewModel(Action<object> cambiarVista)
            : this(
                cambiarVista,
                new PacienteRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public NuevoPacienteViewModel(
            Action<object> cambiarVista,
            PacienteRepository pacienteRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Registrar Nuevo Paciente";
            FechaNacimiento = DateTime.Today.AddYears(-20);

            GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync(), _ => !EstaCargando);
            CancelarCommand = new RelayCommand(_ => Volver());
        }

        private async Task GuardarAsync()
        {
            MensajeError = string.Empty;

            if (!ValidarFormulario())
            {
                MensajeError = "Revisa los campos marcados antes de guardar.";
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(DUI) &&
                        await _pacienteRepository.ExisteDuiEnOtroPacienteAsync(DUI))
                    {
                        MensajeError = "Ya existe un expediente registrado con ese DUI.";
                        return;
                    }

                    var paciente = CrearModelo(
                        idPaciente: 0,
                        activo: true,
                        fechaRegistro: DateTime.Now);

                    int idPaciente = await _pacienteRepository.InsertarAsync(paciente);

                    if (idPaciente <= 0)
                    {
                        MensajeError = "No fue posible confirmar la creación del expediente.";
                        return;
                    }

                    _messageService.MostrarExito(
                        $"El expediente de {paciente.NombreCompleto} fue creado correctamente.",
                        "Paciente registrado");

                    Volver();
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible registrar al paciente.");
                }
            });
        }

        private void Volver()
        {
            var vistaLista = new ListaPacientesView
            {
                DataContext = new ListaPacientesViewModel(_cambiarVista)
            };

            _cambiarVista(vistaLista);
        }
    }
}
