using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Archivos;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.ViewModel.Odontograma;
using ClinicaDentalMario.ViewModel.Tratamientos;
using ClinicaDentalMario.Views.Pacientes;
using ClinicaDentalMario.Views.Tratamientos;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
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
        private readonly PdfService _pdfService = new();

        private int _versionCarga;
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
        public AsyncRelayCommand VistaPreviaExpedienteCommand { get; }
        public AsyncRelayCommand ExportarExpedienteCommand { get; }

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
            OdontogramaVM = new OdontogramaViewModel(
                PacienteActual.IdPaciente,
                PacienteActual.NombreCompleto);

            AbrirNuevaConsultaCommand = new RelayCommand(_ => AbrirNuevaConsulta());
            AbrirNuevoTratamientoCommand = new RelayCommand(_ => AbrirNuevoTratamiento());
            EditarPacienteCommand = new RelayCommand(_ => EditarPaciente());
            VolverCommand = new RelayCommand(_ => Volver());
            VerDetalleConsultaCommand = new RelayCommand(VerDetalleConsulta);
            RecargarCommand = new AsyncRelayCommand(_ => CargarExpedienteAsync());
            VistaPreviaExpedienteCommand = new AsyncRelayCommand(_ => AbrirVistaPreviaAsync());
            ExportarExpedienteCommand = new AsyncRelayCommand(_ => ExportarExpedienteAsync());

            _ = CargarExpedienteAsync();
        }

        private async Task CargarExpedienteAsync()
        {
            int versionActual = Interlocked.Increment(ref _versionCarga);

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

                if (versionActual != Volatile.Read(ref _versionCarga))
                {
                    return;
                }

                List<HistorialClinicoModel> historial = (await historialTask).ToList();
                HistorialConsultas = new ObservableCollection<HistorialClinicoModel>(historial);
                AntecedentesGenerales = await antecedentesTask;

                CargarFallbackAntecedentesLegados(historial);
            }
            catch (Exception ex)
            {
                if (versionActual != Volatile.Read(ref _versionCarga))
                {
                    return;
                }

                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar el expediente clínico.");
            }
            finally
            {
                if (versionActual == Volatile.Read(ref _versionCarga))
                {
                    EstaCargando = false;
                    OnPropertyChanged(nameof(SinConsultas));
                }
            }
        }

        private void CargarFallbackAntecedentesLegados(IEnumerable<HistorialClinicoModel> historial)
        {
            _antecedenteMedicoLegado = historial
                .Select(x => x.AntecedentesMedicos)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            _antecedenteOdontologicoLegado = historial
                .Select(x => x.AntecedentesOdontologicos)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

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

        private void AbrirNuevaConsulta()
        {
            var vista = new NuevaConsultaView
            {
                DataContext = new NuevaConsultaViewModel(PacienteActual, _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void AbrirNuevoTratamiento()
        {
            var vista = new NuevoTratamientoView
            {
                DataContext = new NuevoTratamientoViewModel(PacienteActual, _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void EditarPaciente()
        {
            var vista = new EditarPacienteView
            {
                DataContext = new EditarPacienteViewModel(PacienteActual, _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void Volver()
        {
            var vista = new ListaPacientesView
            {
                DataContext = new ListaPacientesViewModel(_cambiarVista)
            };

            _cambiarVista(vista);
        }

        private void VerDetalleConsulta(object? parameter)
        {
            if (parameter is not HistorialClinicoModel consulta)
            {
                return;
            }

            var vista = new DetalleConsultaView
            {
                DataContext = new DetalleConsultaViewModel(
                    PacienteActual,
                    consulta,
                    _cambiarVista)
            };

            _cambiarVista(vista);
        }

        private async Task AbrirVistaPreviaAsync()
        {
            await ExportarExpedienteInternoAsync(abrirDespues: true);
        }

        private async Task ExportarExpedienteAsync()
        {
            await ExportarExpedienteInternoAsync(abrirDespues: false);
        }

        private async Task ExportarExpedienteInternoAsync(bool abrirDespues)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Guardar expediente clínico",
                    Filter = "Documento PDF (*.pdf)|*.pdf",
                    FileName = $"Expediente_{SanitizarNombreArchivo(PacienteActual.NombreCompleto)}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (!abrirDespues && saveDialog.ShowDialog() != true)
                {
                    return;
                }

                string rutaDestino;
                if (abrirDespues)
                {
                    rutaDestino = Path.Combine(
                        Path.GetTempPath(),
                        $"Expediente_{PacienteActual.IdPaciente}_{Guid.NewGuid():N}.pdf");
                }
                else
                {
                    rutaDestino = saveDialog.FileName;
                }

                await _pdfService.GenerarExpedienteClinicoAsync(
                    PacienteActual,
                    AntecedentesGenerales,
                    HistorialConsultas,
                    rutaDestino);

                if (abrirDespues)
                {
                    _pdfService.AbrirPdf(rutaDestino);
                }
                else
                {
                    _messageService.MostrarExito(
                        "El expediente clínico fue exportado correctamente.",
                        "Expediente exportado");
                }
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(
                    ex,
                    "No fue posible generar el expediente clínico en PDF.");
            }
        }

        private static string SanitizarNombreArchivo(string nombre)
        {
            char[] invalidos = Path.GetInvalidFileNameChars();
            return string.Concat(nombre.Select(c => invalidos.Contains(c) ? '_' : c));
        }
    }
}
