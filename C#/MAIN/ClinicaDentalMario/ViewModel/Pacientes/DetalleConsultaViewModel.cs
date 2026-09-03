using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.Validators;
using ClinicaDentalMario.ViewModel.Base;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class DetalleConsultaViewModel : ValidatableViewModelBase
    {
        private readonly HistorialClinicoRepository _historialRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly HistorialClinicoModel _consultaOriginal;

        public int IdHistorial => _consultaOriginal.IdHistorial;
        public DateTime FechaConsulta => _consultaOriginal.FechaConsulta;
        public string Doctor => string.IsNullOrWhiteSpace(_consultaOriginal.Doctor)
            ? "No especificado"
            : _consultaOriginal.Doctor!;
        public string CambiosAntecedentesMedicos => string.IsNullOrWhiteSpace(_consultaOriginal.AntecedentesMedicos)
            ? "Sin cambios médicos registrados en esta consulta."
            : _consultaOriginal.AntecedentesMedicos!;
        public string CambiosAntecedentesOdontologicos => string.IsNullOrWhiteSpace(_consultaOriginal.AntecedentesOdontologicos)
            ? "Sin cambios odontológicos registrados en esta consulta."
            : _consultaOriginal.AntecedentesOdontologicos!;

        private string _motivoConsulta;
        public string MotivoConsulta
        {
            get => _motivoConsulta;
            set
            {
                if (SetProperty(ref _motivoConsulta, value))
                {
                    ValidarMotivo();
                }
            }
        }

        private string _diagnostico;
        public string Diagnostico
        {
            get => _diagnostico;
            set
            {
                if (SetProperty(ref _diagnostico, value))
                {
                    ValidarDiagnostico();
                }
            }
        }

        private string _planTratamiento;
        public string PlanTratamiento
        {
            get => _planTratamiento;
            set
            {
                if (SetProperty(ref _planTratamiento, value))
                {
                    ValidarPlanTratamiento();
                }
            }
        }

        private string? _observaciones;
        public string? Observaciones
        {
            get => _observaciones;
            set => SetProperty(ref _observaciones, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public bool CambiosGuardados { get; private set; }

        public AsyncRelayCommand GuardarCambiosCommand { get; }
        public ICommand CerrarCommand { get; }

        public DetalleConsultaViewModel(HistorialClinicoModel consulta)
            : this(
                consulta,
                new HistorialClinicoRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public DetalleConsultaViewModel(
            HistorialClinicoModel consulta,
            HistorialClinicoRepository historialRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            ArgumentNullException.ThrowIfNull(consulta);

            if (consulta.IdHistorial <= 0)
            {
                throw new ArgumentException("La consulta no tiene un identificador válido.", nameof(consulta));
            }

            _consultaOriginal = consulta;
            _historialRepository = historialRepository ?? throw new ArgumentNullException(nameof(historialRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Detalle de Consulta Clínica";
            _motivoConsulta = consulta.MotivoConsulta ?? string.Empty;
            _diagnostico = consulta.Diagnostico ?? string.Empty;
            _planTratamiento = consulta.PlanTratamiento ?? string.Empty;
            _observaciones = consulta.Observaciones;

            GuardarCambiosCommand = new AsyncRelayCommand(
                GuardarCambiosAsync,
                _ => !EstaCargando);
            CerrarCommand = new RelayCommand(CerrarVentana);
        }

        private async Task GuardarCambiosAsync(object? parameter)
        {
            MensajeError = string.Empty;

            if (!ValidarFormulario())
            {
                MensajeError = "Revisa los campos marcados antes de guardar los cambios.";
                return;
            }

            EstaCargando = true;
            GuardarCambiosCommand.NotificarCanExecuteChanged();

            try
            {
                var consultaActualizada = new HistorialClinicoModel
                {
                    IdHistorial = _consultaOriginal.IdHistorial,
                    IdPaciente = _consultaOriginal.IdPaciente,
                    IdDoctor = _consultaOriginal.IdDoctor,
                    Doctor = _consultaOriginal.Doctor,
                    FechaConsulta = _consultaOriginal.FechaConsulta,
                    AntecedentesMedicos = _consultaOriginal.AntecedentesMedicos,
                    AntecedentesOdontologicos = _consultaOriginal.AntecedentesOdontologicos,
                    MotivoConsulta = MotivoConsulta.Trim(),
                    Diagnostico = Diagnostico.Trim(),
                    PlanTratamiento = PlanTratamiento.Trim(),
                    Observaciones = LimpiarOpcional(Observaciones)
                };

                await _historialRepository.EditarConsultaAsync(consultaActualizada);

                CambiosGuardados = true;
                _messageService.MostrarExito(
                    "La consulta clínica fue actualizada correctamente.",
                    "Consulta actualizada");

                CerrarVentana(parameter);
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar la consulta clínica.");
            }
            finally
            {
                EstaCargando = false;
                GuardarCambiosCommand.NotificarCanExecuteChanged();
            }
        }

        private bool ValidarFormulario()
        {
            ValidarMotivo();
            ValidarDiagnostico();
            ValidarPlanTratamiento();
            return !HasErrors;
        }

        private void ValidarMotivo()
        {
            ValidarCampo(
                ValidationRules.Requerido(MotivoConsulta, "El motivo de consulta"),
                nameof(MotivoConsulta));
        }

        private void ValidarDiagnostico()
        {
            ValidarCampo(
                ValidationRules.Requerido(Diagnostico, "El diagnóstico"),
                nameof(Diagnostico));
        }

        private void ValidarPlanTratamiento()
        {
            ValidarCampo(
                ValidationRules.Requerido(PlanTratamiento, "El plan de tratamiento"),
                nameof(PlanTratamiento));
        }

        private static string? LimpiarOpcional(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static void CerrarVentana(object? parameter)
        {
            if (parameter is Window ventana)
            {
                ventana.Close();
            }
        }
    }
}
