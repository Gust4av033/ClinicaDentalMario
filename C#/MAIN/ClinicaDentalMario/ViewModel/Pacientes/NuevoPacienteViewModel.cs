using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes; // Asegúrate de tener este using para la navegación
using System.Collections.ObjectModel;
using System.Windows; // Para el MessageBox
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class NuevoPacienteViewModel : ViewModelBase
    {
        private readonly PacienteRepository _pacienteRepo;
        private readonly HistorialClinicoRepository _historialRepo;
        private readonly TratamientoRepository _tratamientoRepo;
        private readonly CatalogoRepository _catalogoRepo;

        // Delegado de navegación
        private readonly Action<object> _cambiarVista;

        private PacienteModel _nuevoPaciente;
        public PacienteModel NuevoPaciente
        {
            get => _nuevoPaciente;
            set => SetProperty(ref _nuevoPaciente, value);
        }

        private ObservableCollection<CatalogoTratamientosModel> _listaTratamientos = new();
        public ObservableCollection<CatalogoTratamientosModel> ListaTratamientos
        {
            get => _listaTratamientos;
            set => SetProperty(ref _listaTratamientos, value);
        }

        private HistorialClinicoModel _nuevoHistorial = new();
        public HistorialClinicoModel NuevoHistorial
        {
            get => _nuevoHistorial;
            set => SetProperty(ref _nuevoHistorial, value);
        }

        private CatalogoTratamientosModel _tratamientoSeleccionado;
        public CatalogoTratamientosModel TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (SetProperty(ref _tratamientoSeleccionado, value))
                {
                    CostoTratamientoInicial = _tratamientoSeleccionado != null ? _tratamientoSeleccionado.PrecioBase : 0;
                }
            }
        }

        private decimal _costoTratamientoInicial;
        public decimal CostoTratamientoInicial
        {
            get => _costoTratamientoInicial;
            set => SetProperty(ref _costoTratamientoInicial, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        // Recibimos la acción de cambiar vista desde quien llame a este ViewModel
        public NuevoPacienteViewModel(Action<object> cambiarVista)
        {
            Titulo = "Registrar Nuevo Paciente";
            _cambiarVista = cambiarVista;

            _pacienteRepo = new PacienteRepository();
            _historialRepo = new HistorialClinicoRepository();
            _tratamientoRepo = new TratamientoRepository();
            _catalogoRepo = new CatalogoRepository();

            _nuevoPaciente = new PacienteModel
            {
                FechaRegistro = DateTime.Now,
                FechaNacimiento = DateTime.Now.AddYears(-20),
                Activo = true
            };

            GuardarCommand = new RelayCommand(async (param) => await GuardarAsync());
            CancelarCommand = new RelayCommand(Volver);

            _ = CargarCatalogosAsync();
        }

        private async Task CargarCatalogosAsync()
        {
            try
            {
                var tratamientos = await _catalogoRepo.ObtenerTratamientosActivosAsync();
                ListaTratamientos = new ObservableCollection<CatalogoTratamientosModel>(tratamientos);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al cargar catálogo: " + ex.Message;
            }
        }

        private async Task GuardarAsync()
        {
            try
            {
                MensajeError = string.Empty;

                if (string.IsNullOrWhiteSpace(NuevoPaciente.NombreCompleto))
                {
                    MensajeError = "El nombre completo del paciente es obligatorio.";
                    return;
                }

                EstaCargando = true;

                // --- 1. VALIDACIÓN DE DUPLICADOS ---
                if (!string.IsNullOrWhiteSpace(NuevoPaciente.DUI))
                {
                    var matchDUI = await _pacienteRepo.BuscarAsync(NuevoPaciente.DUI);
                    if (matchDUI.Any())
                    {
                        MensajeError = "Ya existe un paciente registrado con este DUI.";
                        EstaCargando = false;
                        return;
                    }
                }
                else
                {
                    var matchNombre = await _pacienteRepo.BuscarAsync(NuevoPaciente.NombreCompleto);
                    // Comprobación exacta para no bloquear "Juan Pérez" si ya existe "Juan Pérez García"
                    if (matchNombre.Any(p => p.NombreCompleto.Equals(NuevoPaciente.NombreCompleto, StringComparison.OrdinalIgnoreCase)))
                    {
                        MensajeError = "Ya existe un paciente registrado con ese mismo nombre exacto.";
                        EstaCargando = false;
                        return;
                    }
                }
                // -----------------------------------

                // PASO 1: Guardar Paciente
                int idPacienteGenerado = await _pacienteRepo.InsertarAsync(NuevoPaciente);
                int idDoctorActual = 1;

                // PASO 2: Guardar Historial
                var historialInicial = new HistorialClinicoModel
                {
                    IdPaciente = idPacienteGenerado,
                    IdDoctor = 1,
                    MotivoConsulta = NuevoHistorial.MotivoConsulta,
                    AntecedentesMedicos = NuevoHistorial.AntecedentesMedicos,
                    AntecedentesOdontologicos = NuevoHistorial.AntecedentesOdontologicos,
                    Diagnostico = string.IsNullOrWhiteSpace(NuevoHistorial.Diagnostico) ? "Evaluación Inicial" : NuevoHistorial.Diagnostico,
                    PlanTratamiento = TratamientoSeleccionado != null ? TratamientoSeleccionado.Nombre : "Plan base de apertura",
                    Observaciones = "Registro inicial de expediente",
                    FechaConsulta = DateTime.Now
                };
                await _historialRepo.InsertarConsultaAsync(historialInicial);

                // PASO 3: Guardar Tratamiento
                if (TratamientoSeleccionado != null && TratamientoSeleccionado.IdTratamiento > 0)
                {
                    await _tratamientoRepo.CrearTratamientoAsync(
                        new TratamientoPacienteModel
                        {
                            IdPaciente = idPacienteGenerado,
                            IdDoctor = 1,
                            IdTratamiento = TratamientoSeleccionado.IdTratamiento,
                            CostoTotal = CostoTratamientoInicial,
                            Observaciones = "Tratamiento inicial",
                            Estado = "En progreso",       // 🔥 OBLIGATORIO: Para que la búsqueda de abonos lo reconozca
                            FechaInicio = DateTime.Now   // 🔥 Para registrar la fecha de inicio
                        });
                }

                EstaCargando = false;

                // --- 2. MENSAJE DE ÉXITO ---
                MessageBox.Show($"¡El expediente de {NuevoPaciente.NombreCompleto} ha sido creado con éxito!",
                                "Paciente Guardado",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // --- 3. REGRESAR A LA LISTA ---
                Volver(null);
            }
            catch (Exception ex)
            {
                EstaCargando = false;
                MensajeError = "Error al guardar: " + ex.Message;
            }
        }

        // Método general para "Regresar" o "Atrás"
        private void Volver(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaLista = new ListaPacientesView();
                vistaLista.DataContext = new ListaPacientesViewModel(_cambiarVista);
                _cambiarVista(vistaLista);
            }
        }
    }
}