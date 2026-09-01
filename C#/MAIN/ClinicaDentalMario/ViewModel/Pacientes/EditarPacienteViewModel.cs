using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.Views.Pacientes;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class EditarPacienteViewModel : PacienteFormViewModelBase
    {
        private readonly PacienteRepository _pacienteRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly Action<object> _cambiarVista;

        private readonly int _idPaciente;
        private readonly DateTime _fechaRegistro;

        private bool _activo;
        public bool Activo
        {
            get => _activo;
            private set => SetProperty(ref _activo, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand GuardarCambiosCommand { get; }
        public AsyncRelayCommand CambiarEstadoCommand { get; }
        public ICommand RegresarCommand { get; }

        public EditarPacienteViewModel(
            PacienteModel pacienteSeleccionado,
            Action<object> cambiarVista)
            : this(
                pacienteSeleccionado,
                cambiarVista,
                new PacienteRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public EditarPacienteViewModel(
            PacienteModel pacienteSeleccionado,
            Action<object> cambiarVista,
            PacienteRepository pacienteRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            ArgumentNullException.ThrowIfNull(pacienteSeleccionado);

            if (pacienteSeleccionado.IdPaciente <= 0)
            {
                throw new ArgumentException("El paciente seleccionado no tiene un identificador válido.", nameof(pacienteSeleccionado));
            }

            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            _idPaciente = pacienteSeleccionado.IdPaciente;
            _fechaRegistro = pacienteSeleccionado.FechaRegistro;
            Activo = pacienteSeleccionado.Activo;

            Titulo = "Editar Paciente";
            CargarPaciente(pacienteSeleccionado);

            GuardarCambiosCommand = new AsyncRelayCommand(_ => GuardarCambiosAsync(), _ => !EstaCargando);
            CambiarEstadoCommand = new AsyncRelayCommand(_ => CambiarEstadoAsync(), _ => !EstaCargando);
            RegresarCommand = new RelayCommand(_ => Volver());
        }

        private async Task GuardarCambiosAsync()
        {
            MensajeError = string.Empty;

            if (!ValidarFormulario())
            {
                MensajeError = "Revisa los campos marcados antes de actualizar.";
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(DUI) &&
                        await _pacienteRepository.ExisteDuiEnOtroPacienteAsync(DUI, _idPaciente))
                    {
                        MensajeError = "Ese DUI pertenece a otro expediente registrado.";
                        return;
                    }

                    var paciente = CrearModelo(
                        _idPaciente,
                        Activo,
                        _fechaRegistro);

                    await _pacienteRepository.ActualizarAsync(paciente);

                    _messageService.MostrarExito(
                        "Los datos del paciente fueron actualizados correctamente.",
                        "Paciente actualizado");

                    Volver();
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible actualizar al paciente.");
                }
            });
        }

        private async Task CambiarEstadoAsync()
        {
            string accion = Activo ? "desactivar" : "restaurar";
            string pregunta = Activo
                ? $"¿Deseas desactivar el expediente de {NombreCompleto}?"
                : $"¿Deseas restaurar el expediente de {NombreCompleto}?";

            if (!_messageService.Confirmar(pregunta, char.ToUpperInvariant(accion[0]) + accion[1..]))
            {
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    if (Activo)
                    {
                        await _pacienteRepository.EliminarAsync(_idPaciente);
                        Activo = false;
                    }
                    else
                    {
                        await _pacienteRepository.RestaurarAsync(_idPaciente);
                        Activo = true;
                    }

                    _messageService.MostrarExito(
                        Activo
                            ? "El expediente fue restaurado correctamente."
                            : "El expediente fue desactivado correctamente.",
                        "Estado actualizado");

                    Volver();
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        $"No fue posible {accion} el expediente.");
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
