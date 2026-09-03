using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Archivos;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.ViewModel.Odontograma;
using ClinicaDentalMario.ViewModel.Tratamientos;
using ClinicaDentalMario.Views.Pacientes;
using ClinicaDentalMario.Views.Tratamientos;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class HistorialPacienteViewModel : ViewModelBase
    {
        private readonly HistorialClinicoRepository _historialRepo;
        private readonly PacienteRepository _pacienteRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly Action<object> _cambiarVista;

        private string? _antecedenteMedicoLegado;
        private string? _antecedenteOdontologicoLegado;

        private PacienteModel _pacienteActual;
        public PacienteModel PacienteActual
        {
            get => _pacienteActual;
            private set => SetProperty(ref _pacienteActual, value);
        }

        private AntecedentesPacienteModel? _antecedentesGenerales;
        public AntecedentesPacienteModel? AntecedentesGenerales
        {
            get => _antecedentesGenerales;
            private set
            {
                if (SetProperty(ref _antecedentesGenerales, value))
                {
                    NotificarCambioAntecedentes();
                }
            }
        }

        public bool TieneAntecedentesRegistrados => AntecedentesGenerales is not null;

        public bool UsaAntecedentesLegados =>
            AntecedentesGenerales is null &&
            (!string.IsNullOrWhiteSpace(_antecedenteMedicoLegado) ||
             !string.IsNullOrWhiteSpace(_antecedenteOdontologicoLegado));

        public string ResumenAntecedentesMedicos
        {
            get
            {
                if (AntecedentesGenerales is not null)
                {
                    if (!AntecedentesGenerales.TieneAntecedentesMedicos)
                    {
                        return "Sin antecedentes médicos registrados";
                    }

                    return string.IsNullOrWhiteSpace(AntecedentesGenerales.DetalleAntecedentesMedicos)
                        ? "Antecedentes médicos indicados sin detalle"
                        : AntecedentesGenerales.DetalleAntecedentesMedicos!;
                }

                return !string.IsNullOrWhiteSpace(_antecedenteMedicoLegado)
                    ? _antecedenteMedicoLegado!
                    : "Pendiente de registrar";
            }
        }

        public string ResumenAntecedentesOdontologicos
        {
            get
            {
                if (AntecedentesGenerales is not null)
                {
                    if (!AntecedentesGenerales.TieneAntecedentesOdontologicos)
                    {
                        return "Sin antecedentes odontológicos registrados";
                    }

                    return string.IsNullOrWhiteSpace(AntecedentesGenerales.DetalleAntecedentesOdontologicos)
                        ? "Antecedentes odontológicos indicados sin detalle"
                        : AntecedentesGenerales.DetalleAntecedentesOdontologicos!;
                }

                return !string.IsNullOrWhiteSpace(_antecedenteOdontologicoLegado)
                    ? _antecedenteOdontologicoLegado!
                    : "Pendiente de registrar";
            }
        }

        public string MensajeFuenteAntecedentes => AntecedentesGenerales is not null
            ? string.Empty
            : UsaAntecedentesLegados
                ? "Mostrando antecedentes recuperados de consultas anteriores. Conviene confirmarlos desde Editar Paciente para convertirlos en antecedentes generales vigentes."
                : "Este paciente todavía no tiene antecedentes generales confirmados ni se encontraron antecedentes previos. Puedes registrarlos desde Editar Paciente.";

        private ObservableCollection<HistorialClinicoModel> _historialConsultas = new();
        public ObservableCollection<HistorialClinicoModel> HistorialConsultas
        {
            get => _historialConsultas;
            private set
            {
                if (SetProperty(ref _historialConsultas, value))
                {
                    OnPropertyChanged(nameof(SinConsultas));
                }
            }
        }

        public bool SinConsultas => !EstaCargando && HistorialConsultas.Count == 0;

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public ImagenesPacienteViewModel GaleriaVM { get; }
        public OdontogramaViewModel OdontogramaVM { get; }

        public ICommand AbrirNuevaConsultaCommand { get; }
        public ICommand AbrirNuevoTratamientoCommand { get; }
        public ICommand EditarPacienteCommand { get; }
        public ICommand VolverCommand { get; }
        public ICommand VerDetalleConsultaCommand { get; }
        public AsyncRelayCommand RecargarCommand { get; }

        public HistorialPacienteViewModel(PacienteModel paciente, Action<object> cambiarVista)
            : this(
                paciente,
                cambiarVista,
                new HistorialClinicoRepository(),
                new PacienteRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public HistorialPacienteViewModel(
            PacienteModel paciente,
            Action<object> cambiarVista,
            HistorialClinicoRepository historialRepo,
            PacienteRepository pacienteRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            ArgumentNullException.ThrowIfNull(paciente);
            if (paciente.IdPaciente <= 0)
            {
                throw new ArgumentException("El paciente no tiene un identificador válido.", nameof(paciente));
            }

            _pacienteActual = paciente;
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _historialRepo = historialRepo ?? throw new ArgumentNullException(nameof(historialRepo));
            _pacienteRepository = pacienteRepository ?? throw new ArgumentNullException(nameof(pacienteRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = $"Expediente Clínico - {paciente.NombreCompleto}";

            GaleriaVM = new ImagenesPacienteViewModel(PacienteActual.IdPaciente, _cambiarVista);
            OdontogramaVM = new OdontogramaViewModel(PacienteActual.IdPaciente);

            AbrirNuevaConsultaCommand = new RelayCommand(_ => AbrirNuevaConsulta());
            AbrirNuevoTratamientoCommand = new RelayCommand(_ => AbrirNuevoTratamiento());
            EditarPacienteCommand = new RelayCommand(_ => EditarPaciente());
            VolverCommand = new RelayCommand(_ => Volver());
            VerDetalleConsultaCommand = new RelayCommand(VerDetalleConsulta);
            RecargarCommand = new AsyncRelayCommand(_ => CargarExpedienteAsync());

            _ = CargarExpedienteAsync();
        }

        private async Task CargarExpedienteAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;
            OnPropertyChanged(nameof(SinConsultas));

            try
            {
                Task<IEnumerable<HistorialClinicoModel>> historialTask =
                    _historialRepo.ListarConsultasAsync(PacienteActual.IdPaciente);
                Task<AntecedentesPacienteModel?> antecedentesTask =
                    _pacienteRepository.ObtenerAntecedentesAsync(PacienteActual.IdPaciente);

                await Task.WhenAll(historialTask, antecedentesTask);

                List<HistorialClinicoModel> historial = (await historialTask).ToList();
                HistorialConsultas = new ObservableCollection<HistorialClinicoModel>(historial);
                AntecedentesGenerales = await antecedentesTask;

                CargarFallbackAntecedentesLegados(historial);
            }
            catch (Exception ex)
            {
                HistorialConsultas = new ObservableCollection<HistorialClinicoModel>();
                AntecedentesGenerales = null;
                _antecedenteMedicoLegado = null;
                _antecedenteOdontologicoLegado = null;
                NotificarCambioAntecedentes();

                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar el expediente clínico del paciente.");
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinConsultas));
            }
        }

        private void CargarFallbackAntecedentesLegados(IEnumerable<HistorialClinicoModel> historial)
        {
            _antecedenteMedicoLegado = null;
            _antecedenteOdontologicoLegado = null;

            if (AntecedentesGenerales is null)
            {
                foreach (HistorialClinicoModel consulta in historial.OrderByDescending(x => x.FechaConsulta))
                {
                    if (string.IsNullOrWhiteSpace(_antecedenteMedicoLegado) &&
                        !string.IsNullOrWhiteSpace(consulta.AntecedentesMedicos))
                    {
                        _antecedenteMedicoLegado = consulta.AntecedentesMedicos.Trim();
                    }

                    if (string.IsNullOrWhiteSpace(_antecedenteOdontologicoLegado) &&
                        !string.IsNullOrWhiteSpace(consulta.AntecedentesOdontologicos))
                    {
                        _antecedenteOdontologicoLegado = consulta.AntecedentesOdontologicos.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(_antecedenteMedicoLegado) &&
                        !string.IsNullOrWhiteSpace(_antecedenteOdontologicoLegado))
                    {
                        break;
                    }
                }
            }

            NotificarCambioAntecedentes();
        }

        private void NotificarCambioAntecedentes()
        {
            OnPropertyChanged(nameof(TieneAntecedentesRegistrados));
            OnPropertyChanged(nameof(UsaAntecedentesLegados));
            OnPropertyChanged(nameof(ResumenAntecedentesMedicos));
            OnPropertyChanged(nameof(ResumenAntecedentesOdontologicos));
            OnPropertyChanged(nameof(MensajeFuenteAntecedentes));
        }

        private void EditarPaciente()
        {
            try
            {
                var vista = new EditarPacienteView
                {
                    DataContext = new EditarPacienteViewModel(
                        PacienteActual,
                        _cambiarVista,
                        RegresarDesdeEdicion)
                };

                _cambiarVista(vista);
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible abrir la edición del paciente.");
            }
        }

        private void RegresarDesdeEdicion(PacienteModel pacienteActualizado)
        {
            var vista = new HistorialPacienteView
            {
                DataContext = new HistorialPacienteViewModel(
                    pacienteActualizado,
                    _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void AbrirNuevaConsulta()
        {
            try
            {
                var vm = new NuevaConsultaViewModel(
                    PacienteActual.IdPaciente,
                    PacienteActual.NombreCompleto);

                var ventana = new NuevaConsultaWindow
                {
                    DataContext = vm
                };

                ventana.ShowDialog();

                if (!vm.ConsultaGuardada)
                {
                    return;
                }

                _ = CargarExpedienteAsync();

                if (vm.DeseaAsignarTratamiento)
                {
                    AbrirNuevoTratamiento();
                }
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible abrir el registro de una nueva consulta.");
            }
        }

        private void AbrirNuevoTratamiento()
        {
            try
            {
                var vista = new NuevoTratamientoView
                {
                    DataContext = new NuevoTratamientoViewModel(
                        PacienteActual.IdPaciente,
                        PacienteActual.NombreCompleto,
                        _cambiarVista)
                };

                _cambiarVista(vista);
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible abrir el registro de tratamiento.");
            }
        }

        private void Volver()
        {
            var vistaLista = new ListaPacientesView
            {
                DataContext = new ListaPacientesViewModel(_cambiarVista)
            };

            _cambiarVista(vistaLista);
        }

        private void VerDetalleConsulta(object? parameter)
        {
            if (parameter is not HistorialClinicoModel consulta)
            {
                return;
            }

            try
            {
                var vm = new DetalleConsultaViewModel(
                    consulta,
                    _historialRepo,
                    _messageService,
                    _exceptionHandler);

                var ventana = new DetalleConsultaWindow
                {
                    DataContext = vm
                };

                ventana.ShowDialog();

                if (vm.CambiosGuardados)
                {
                    _ = CargarExpedienteAsync();
                }
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible abrir el detalle de la consulta clínica.");
            }
        }
    }
}
