using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.Validators;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class NuevaConsultaViewModel : ValidatableViewModelBase
    {
        private readonly HistorialClinicoRepository _historialRepo;
        private readonly DoctorRepository _doctorRepository;
        private readonly PacienteRepository _pacienteRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        public int IdPaciente { get; }
        public string NombrePaciente { get; }

        private ObservableCollection<DoctorModel> _listaDoctores = new();
        public ObservableCollection<DoctorModel> ListaDoctores
        {
            get => _listaDoctores;
            private set => SetProperty(ref _listaDoctores, value);
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

        private AntecedentesPacienteModel? _antecedentesGenerales;
        public AntecedentesPacienteModel? AntecedentesGenerales
        {
            get => _antecedentesGenerales;
            private set
            {
                if (SetProperty(ref _antecedentesGenerales, value))
                {
                    OnPropertyChanged(nameof(ResumenAntecedentesMedicos));
                    OnPropertyChanged(nameof(ResumenAntecedentesOdontologicos));
                    OnPropertyChanged(nameof(TieneAntecedentesGenerales));
                }
            }
        }

        public bool TieneAntecedentesGenerales => AntecedentesGenerales is not null;

        public string ResumenAntecedentesMedicos => AntecedentesGenerales switch
        {
            null => "No existe ficha general de antecedentes.",
            { TieneAntecedentesMedicos: false } => "Sin antecedentes médicos registrados.",
            _ => string.IsNullOrWhiteSpace(AntecedentesGenerales.DetalleAntecedentesMedicos)
                ? "Antecedentes médicos indicados sin detalle."
                : AntecedentesGenerales.DetalleAntecedentesMedicos!
        };

        public string ResumenAntecedentesOdontologicos => AntecedentesGenerales switch
        {
            null => "No existe ficha general de antecedentes.",
            { TieneAntecedentesOdontologicos: false } => "Sin antecedentes odontológicos registrados.",
            _ => string.IsNullOrWhiteSpace(AntecedentesGenerales.DetalleAntecedentesOdontologicos)
                ? "Antecedentes odontológicos indicados sin detalle."
                : AntecedentesGenerales.DetalleAntecedentesOdontologicos!
        };

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        private string _motivoConsulta = string.Empty;
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

        private string? _cambiosAntecedentesMedicos;
        public string? CambiosAntecedentesMedicos
        {
            get => _cambiosAntecedentesMedicos;
            set => SetProperty(ref _cambiosAntecedentesMedicos, value);
        }

        private string? _cambiosAntecedentesOdontologicos;
        public string? CambiosAntecedentesOdontologicos
        {
            get => _cambiosAntecedentesOdontologicos;
            set => SetProperty(ref _cambiosAntecedentesOdontologicos, value);
        }

        private string _diagnostico = string.Empty;
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

        private string _planTratamiento = string.Empty;
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

        public bool ConsultaGuardada { get; private set; }
        public bool DeseaAsignarTratamiento { get; private set; }

        public AsyncRelayCommand GuardarConsultaCommand { get; }
        public ICommand CerrarVentanaCommand { get; }

        public NuevaConsultaViewModel(int idPaciente, string nombrePaciente)
            : this(
                idPaciente,
                nombrePaciente,
                new HistorialClinicoRepository(),
                new DoctorRepository(),
                new PacienteRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public NuevaConsultaViewModel(
            int idPaciente,
            string nombrePaciente,
            HistorialClinicoRepository historialRepo,
            DoctorRepository doctorRepository,
            PacienteRepository pacienteRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            if (idPaciente <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(idPaciente));
            }

            if (string.IsNullOrWhiteSpace(nombrePaciente))
            {
                throw new ArgumentException("El nombre del paciente es obligatorio.", nameof(nombrePaciente));
            }

            IdPaciente = idPaciente;
            NombrePaciente = nombrePaciente.Trim();
            _historialRepo = historialRepo ?? throw new ArgumentNullException(nameof(historialRepo));
            _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Registrar Nueva Consulta Clínica";

            GuardarConsultaCommand = new AsyncRelayCommand(_ => GuardarAsync(_));
            CerrarVentanaCommand = new RelayCommand(CerrarVentana);

            _ = CargarContextoAsync();
        }

        private async Task CargarContextoAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;

            try
            {
                Task<IEnumerable<DoctorModel>> doctoresTask =
                    _doctorRepository.ObtenerDoctoresActivosAsync();
                Task<AntecedentesPacienteModel?> antecedentesTask =
                    _pacienteRepository.ObtenerAntecedentesAsync(IdPaciente);

                await Task.WhenAll(doctoresTask, antecedentesTask);

                ListaDoctores = new ObservableCollection<DoctorModel>(await doctoresTask);
                AntecedentesGenerales = await antecedentesTask;

                if (ListaDoctores.Count == 1)
                {
                    DoctorSeleccionado = ListaDoctores[0];
                }
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar la información necesaria para registrar la consulta.");
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task GuardarAsync(object? parameter)
        {
            MensajeError = string.Empty;

            if (!ValidarFormulario())
            {
                MensajeError = "Revisa los campos marcados antes de guardar la consulta.";
                return;
            }

            EstaCargando = true;
            try
            {
                var nuevaConsulta = new HistorialClinicoModel
                {
                    IdPaciente = IdPaciente,
                    IdDoctor = DoctorSeleccionado!.IdDoctor,
                    FechaConsulta = DateTime.Now,
                    MotivoConsulta = MotivoConsulta.Trim(),
                    AntecedentesMedicos = LimpiarOpcional(CambiosAntecedentesMedicos),
                    AntecedentesOdontologicos = LimpiarOpcional(CambiosAntecedentesOdontologicos),
                    Diagnostico = Diagnostico.Trim(),
                    PlanTratamiento = PlanTratamiento.Trim(),
                    Observaciones = LimpiarOpcional(Observaciones)
                };

                await _historialRepo.InsertarConsultaAsync(nuevaConsulta);

                ConsultaGuardada = true;
                DeseaAsignarTratamiento = false;
                _messageService.MostrarExito(
                    "La consulta fue agregada correctamente al expediente del paciente.",
                    "Consulta guardada");

                CerrarVentana(parameter);
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible guardar la consulta clínica.");
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private bool ValidarFormulario()
        {
            ValidarDoctor();
            ValidarMotivo();
            ValidarDiagnostico();
            ValidarPlanTratamiento();
            return !HasErrors;
        }

        private void ValidarDoctor()
        {
            if (DoctorSeleccionado is null)
            {
                EstablecerErrores(new[] { "Selecciona el doctor que atendió la consulta." }, nameof(DoctorSeleccionado));
            }
            else
            {
                LimpiarErrores(nameof(DoctorSeleccionado));
            }
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
