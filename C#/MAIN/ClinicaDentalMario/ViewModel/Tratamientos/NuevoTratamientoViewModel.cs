using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Tratamientos;
using System.Collections.ObjectModel;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class NuevoTratamientoViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly int _idPaciente;
        private readonly TratamientoRepository _tratamientoRepository;
        private readonly CatalogoRepository _catalogoRepository;
        private readonly DoctorRepository _doctorRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private TratamientoPacienteModel _nuevoTratamiento;
        public TratamientoPacienteModel NuevoTratamiento
        {
            get => _nuevoTratamiento;
            private set => SetProperty(ref _nuevoTratamiento, value);
        }

        private ObservableCollection<CatalogoTratamientosModel> _listaCatalogo = new();
        public ObservableCollection<CatalogoTratamientosModel> ListaCatalogo
        {
            get => _listaCatalogo;
            private set => SetProperty(ref _listaCatalogo, value);
        }

        private CatalogoTratamientosModel? _tratamientoSeleccionado;
        public CatalogoTratamientosModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (!SetProperty(ref _tratamientoSeleccionado, value))
                    return;

                if (value is not null)
                {
                    NuevoTratamiento.CostoTotal = value.PrecioBase;
                    OnPropertyChanged(nameof(NuevoTratamiento));
                }
            }
        }

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
            set => SetProperty(ref _doctorSeleccionado, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        private string _nombrePacienteTexto = string.Empty;
        public string NombrePacienteTexto
        {
            get => _nombrePacienteTexto;
            private set => SetProperty(ref _nombrePacienteTexto, value);
        }

        public AsyncRelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }

        public NuevoTratamientoViewModel(
            int idPaciente,
            string nombrePaciente,
            Action<object> cambiarVista)
            : this(
                idPaciente,
                nombrePaciente,
                cambiarVista,
                new TratamientoRepository(),
                new CatalogoRepository(),
                new DoctorRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public NuevoTratamientoViewModel(
            int idPaciente,
            string nombrePaciente,
            Action<object> cambiarVista,
            TratamientoRepository tratamientoRepository,
            CatalogoRepository catalogoRepository,
            DoctorRepository doctorRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            if (idPaciente <= 0)
                throw new ArgumentOutOfRangeException(nameof(idPaciente));

            _idPaciente = idPaciente;
            _cambiarVista = cambiarVista ?? throw new ArgumentNullException(nameof(cambiarVista));
            _tratamientoRepository = tratamientoRepository ?? throw new ArgumentNullException(nameof(tratamientoRepository));
            _catalogoRepository = catalogoRepository ?? throw new ArgumentNullException(nameof(catalogoRepository));
            _doctorRepository = doctorRepository ?? throw new ArgumentNullException(nameof(doctorRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Asignar Tratamiento";
            NombrePacienteTexto = $"Paciente: {nombrePaciente}";

            _nuevoTratamiento = new TratamientoPacienteModel
            {
                IdPaciente = idPaciente,
                Estado = "Pendiente",
                Observaciones = string.Empty
            };

            GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync());
            CancelarCommand = new RelayCommand(_ => VolverLista());

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            MensajeError = string.Empty;
            EstaCargando = true;

            try
            {
                Task<IEnumerable<CatalogoTratamientosModel>> catalogoTask =
                    _catalogoRepository.ObtenerTratamientosActivosAsync();
                Task<IEnumerable<DoctorModel>> doctoresTask =
                    _doctorRepository.ObtenerDoctoresActivosAsync();

                await Task.WhenAll(catalogoTask, doctoresTask);

                ListaCatalogo = new ObservableCollection<CatalogoTratamientosModel>(
                    await catalogoTask);
                ListaDoctores = new ObservableCollection<DoctorModel>(
                    await doctoresTask);

                if (ListaDoctores.Count == 1)
                    DoctorSeleccionado = ListaDoctores[0];
            }
            catch (Exception ex)
            {
                ListaCatalogo = new ObservableCollection<CatalogoTratamientosModel>();
                ListaDoctores = new ObservableCollection<DoctorModel>();
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible cargar los datos necesarios para asignar el tratamiento.");
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task GuardarAsync()
        {
            MensajeError = string.Empty;

            if (TratamientoSeleccionado is null)
            {
                MensajeError = "Debe seleccionar un tratamiento del catálogo.";
                return;
            }

            if (DoctorSeleccionado is null)
            {
                MensajeError = "Debe seleccionar el doctor responsable del tratamiento.";
                return;
            }

            if (NuevoTratamiento.CostoTotal < 0)
            {
                MensajeError = "El costo acordado no puede ser negativo.";
                return;
            }

            EstaCargando = true;

            try
            {
                NuevoTratamiento.IdPaciente = _idPaciente;
                NuevoTratamiento.IdDoctor = DoctorSeleccionado.IdDoctor;
                NuevoTratamiento.IdTratamiento = TratamientoSeleccionado.IdTratamiento;
                NuevoTratamiento.Estado = "Pendiente";
                NuevoTratamiento.Observaciones = string.IsNullOrWhiteSpace(NuevoTratamiento.Observaciones)
                    ? null
                    : NuevoTratamiento.Observaciones.Trim();

                await _tratamientoRepository.CrearTratamientoAsync(NuevoTratamiento);

                _messageService.MostrarExito(
                    "Tratamiento asignado correctamente. Quedó en estado Pendiente hasta que se inicie.");

                VolverLista();
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible asignar el tratamiento al paciente.");
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void VolverLista()
        {
            var vistaPrincipal = new TratamientosView
            {
                DataContext = new TratamientosViewModel(_cambiarVista, _idPaciente)
            };

            _cambiarVista(vistaPrincipal);
        }
    }
}
