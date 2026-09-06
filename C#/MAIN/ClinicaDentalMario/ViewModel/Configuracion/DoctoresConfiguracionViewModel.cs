using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Net.Mail;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class DoctoresConfiguracionViewModel : ViewModelBase
    {
        private const string Todos = "Todos";
        private const string Activos = "Activos";
        private const string Inactivos = "Inactivos";

        private readonly DoctorRepository _doctorRepo;
        private readonly BitacoraRepository _bitacoraRepo;
        private readonly IPermissionService _permissionService;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private List<DoctorModel> _todosDoctores = new();

        private ObservableCollection<DoctorModel> _doctores = new();
        public ObservableCollection<DoctorModel> Doctores
        {
            get => _doctores;
            private set
            {
                if (SetProperty(ref _doctores, value))
                    OnPropertyChanged(nameof(SinDoctores));
            }
        }

        public ObservableCollection<string> EstadosFiltro { get; } = new() { Todos, Activos, Inactivos };

        private string _textoBusqueda = string.Empty;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                if (SetProperty(ref _textoBusqueda, value ?? string.Empty))
                    AplicarFiltros();
            }
        }

        private string _estadoSeleccionado = Todos;
        public string EstadoSeleccionado
        {
            get => _estadoSeleccionado;
            set
            {
                if (SetProperty(ref _estadoSeleccionado, value ?? Todos))
                    AplicarFiltros();
            }
        }

        private DoctorModel? _doctorSeleccionado;
        public DoctorModel? DoctorSeleccionado
        {
            get => _doctorSeleccionado;
            set
            {
                if (SetProperty(ref _doctorSeleccionado, value))
                {
                    CargarFormularioDesdeSeleccion();
                    OnPropertyChanged(nameof(EsEdicion));
                    OnPropertyChanged(nameof(TituloFormulario));
                    OnPropertyChanged(nameof(TextoBotonGuardar));
                    OnPropertyChanged(nameof(TextoBotonEstado));
                    OnPropertyChanged(nameof(PuedeCambiarEstado));
                    CambiarEstadoCommand.NotificarCanExecuteChanged();
                }
            }
        }

        private string _nombreCompleto = string.Empty;
        public string NombreCompleto
        {
            get => _nombreCompleto;
            set => SetProperty(ref _nombreCompleto, value ?? string.Empty);
        }

        private string _especialidad = string.Empty;
        public string Especialidad
        {
            get => _especialidad;
            set => SetProperty(ref _especialidad, value ?? string.Empty);
        }

        private string _telefono = string.Empty;
        public string Telefono
        {
            get => _telefono;
            set => SetProperty(ref _telefono, value ?? string.Empty);
        }

        private string _correo = string.Empty;
        public string Correo
        {
            get => _correo;
            set => SetProperty(ref _correo, value ?? string.Empty);
        }

        private string _direccion = string.Empty;
        public string Direccion
        {
            get => _direccion;
            set => SetProperty(ref _direccion, value ?? string.Empty);
        }

        private string _numeroJVPO = string.Empty;
        public string NumeroJVPO
        {
            get => _numeroJVPO;
            set => SetProperty(ref _numeroJVPO, value ?? string.Empty);
        }

        public int TotalDoctores => _todosDoctores.Count;
        public int TotalActivos => _todosDoctores.Count(x => x.Activo);
        public int TotalInactivos => _todosDoctores.Count(x => !x.Activo);
        public bool SinDoctores => !EstaCargando && Doctores.Count == 0;
        public bool EsEdicion => DoctorSeleccionado != null;
        public bool PuedeCambiarEstado => DoctorSeleccionado != null;
        public string TituloFormulario => EsEdicion ? "Editar profesional" : "Registrar profesional";
        public string TextoBotonGuardar => EsEdicion ? "Guardar cambios" : "Registrar doctor";
        public string TextoBotonEstado => DoctorSeleccionado?.Activo == true ? "Desactivar" : "Reactivar";

        public AsyncRelayCommand CargarCommand { get; }
        public AsyncRelayCommand GuardarCommand { get; }
        public AsyncRelayCommand CambiarEstadoCommand { get; }
        public RelayCommand NuevoCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }

        public DoctoresConfiguracionViewModel()
            : this(
                new DoctorRepository(),
                new BitacoraRepository(),
                new PermissionService(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public DoctoresConfiguracionViewModel(
            DoctorRepository doctorRepo,
            BitacoraRepository bitacoraRepo,
            IPermissionService permissionService,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            Titulo = "Doctores y personal clínico";
            _doctorRepo = doctorRepo ?? throw new ArgumentNullException(nameof(doctorRepo));
            _bitacoraRepo = bitacoraRepo ?? throw new ArgumentNullException(nameof(bitacoraRepo));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
                throw new UnauthorizedAccessException("El usuario actual no puede administrar doctores.");

            CargarCommand = new AsyncRelayCommand(_ => CargarAsync());
            GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync());
            CambiarEstadoCommand = new AsyncRelayCommand(
                _ => CambiarEstadoAsync(),
                _ => DoctorSeleccionado != null);
            NuevoCommand = new RelayCommand(_ => PrepararNuevo());
            LimpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());

            _ = CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (EstaCargando)
                return;

            EstaCargando = true;
            LimpiarMensaje();
            OnPropertyChanged(nameof(SinDoctores));

            try
            {
                _todosDoctores = (await _doctorRepo.ObtenerDoctoresAsync()).ToList();
                AplicarFiltros();
                NotificarResumen();
            }
            catch (Exception ex)
            {
                _todosDoctores.Clear();
                Doctores = new ObservableCollection<DoctorModel>();
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "cargar los doctores"));
                NotificarResumen();
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinDoctores));
            }
        }

        private async Task GuardarAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                _messageService.MostrarAdvertencia("No tienes permisos para administrar doctores.", "Acceso denegado");
                return;
            }

            if (!TryConstruirDoctor(out DoctorModel doctor))
                return;

            if (EstaCargando)
                return;

            EstaCargando = true;
            LimpiarMensaje();
            string mensajeExito;

            try
            {
                int? excluirId = DoctorSeleccionado?.IdDoctor;
                if (!string.IsNullOrWhiteSpace(doctor.NumeroJVPO) &&
                    await _doctorRepo.ExisteNumeroJVPOAsync(doctor.NumeroJVPO, excluirId))
                {
                    MostrarAdvertencia("Ya existe otro doctor registrado con ese número JVPO.");
                    return;
                }

                if (DoctorSeleccionado == null)
                {
                    await _doctorRepo.CrearDoctorAsync(doctor);
                    mensajeExito = "Doctor registrado correctamente.";
                    await RegistrarAuditoriaSeguraAsync(
                        "INSERT",
                        $"Doctor creado | Nombre: {doctor.NombreCompleto} | Especialidad: {doctor.Especialidad ?? "Sin especificar"} | JVPO: {doctor.NumeroJVPO ?? "No registrado"}");
                }
                else
                {
                    doctor.IdDoctor = DoctorSeleccionado.IdDoctor;
                    doctor.Activo = DoctorSeleccionado.Activo;
                    await _doctorRepo.ActualizarDoctorAsync(doctor);
                    mensajeExito = "Datos del doctor actualizados correctamente.";
                    await RegistrarAuditoriaSeguraAsync(
                        "UPDATE",
                        $"Doctor actualizado | IdDoctor: {doctor.IdDoctor} | Nombre: {doctor.NombreCompleto} | Especialidad: {doctor.Especialidad ?? "Sin especificar"} | JVPO: {doctor.NumeroJVPO ?? "No registrado"}");
                }
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "guardar el doctor"));
                return;
            }
            finally
            {
                EstaCargando = false;
            }

            PrepararNuevo();
            await CargarAsync();
            MostrarExito(mensajeExito);
        }

        private async Task CambiarEstadoAsync()
        {
            if (DoctorSeleccionado == null)
                return;

            DoctorModel doctor = DoctorSeleccionado;
            bool nuevoEstado = !doctor.Activo;
            string verbo = nuevoEstado ? "reactivar" : "desactivar";

            string mensajeConfirmacion;
            if (!nuevoEstado)
            {
                int citasFuturas;
                try
                {
                    citasFuturas = await _doctorRepo.ContarCitasFuturasAsync(doctor.IdDoctor);
                }
                catch (Exception ex)
                {
                    MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "validar las citas del doctor"));
                    return;
                }

                mensajeConfirmacion = citasFuturas > 0
                    ? $"El doctor tiene {citasFuturas} cita(s) futura(s) pendiente(s) o confirmada(s).\n\n¿Deseas desactivarlo de todos modos? Las citas existentes no se eliminarán, pero ya no aparecerá para nuevas asignaciones."
                    : $"¿Deseas desactivar a '{doctor.NombreCompleto}'?\n\nNo aparecerá para nuevas asignaciones, pero sus historiales y citas existentes se conservarán.";
            }
            else
            {
                mensajeConfirmacion = $"¿Deseas reactivar a '{doctor.NombreCompleto}' para que vuelva a estar disponible en Agenda y Tratamientos?";
            }

            if (!_messageService.Confirmar(mensajeConfirmacion, nuevoEstado ? "Reactivar doctor" : "Desactivar doctor"))
                return;

            try
            {
                await _doctorRepo.CambiarEstadoDoctorAsync(doctor.IdDoctor, nuevoEstado);
                await RegistrarAuditoriaSeguraAsync(
                    "UPDATE",
                    $"Doctor {(nuevoEstado ? "reactivado" : "desactivado")} | IdDoctor: {doctor.IdDoctor} | Nombre: {doctor.NombreCompleto}");

                PrepararNuevo();
                await CargarAsync();
                MostrarExito($"Doctor {(nuevoEstado ? "reactivado" : "desactivado")} correctamente.");
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, verbo + " el doctor"));
            }
        }

        private void AplicarFiltros()
        {
            IEnumerable<DoctorModel> consulta = _todosDoctores;
            string texto = TextoBusqueda.Trim();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    (x.Especialidad?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.NumeroJVPO?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Telefono?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (EstadoSeleccionado == Activos)
                consulta = consulta.Where(x => x.Activo);
            else if (EstadoSeleccionado == Inactivos)
                consulta = consulta.Where(x => !x.Activo);

            Doctores = new ObservableCollection<DoctorModel>(consulta);
        }

        private void LimpiarFiltros()
        {
            TextoBusqueda = string.Empty;
            EstadoSeleccionado = Todos;
            AplicarFiltros();
        }

        private bool TryConstruirDoctor(out DoctorModel doctor)
        {
            doctor = new DoctorModel();
            string nombre = NombreCompleto.Trim();
            string especialidad = Especialidad.Trim();
            string telefono = Telefono.Trim();
            string correo = Correo.Trim();
            string direccion = Direccion.Trim();
            string jvpo = NumeroJVPO.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MostrarAdvertencia("El nombre completo del doctor es obligatorio.");
                return false;
            }

            if (nombre.Length > 150 || especialidad.Length > 80 || telefono.Length > 20 ||
                correo.Length > 120 || direccion.Length > 250 || jvpo.Length > 30)
            {
                MostrarAdvertencia("Uno o más campos exceden la longitud permitida. Revisa nombre, especialidad, teléfono, correo, dirección y JVPO.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(correo) && !MailAddress.TryCreate(correo, out _))
            {
                MostrarAdvertencia("Ingresa un correo electrónico válido o deja el campo vacío.");
                return false;
            }

            doctor = new DoctorModel
            {
                NombreCompleto = nombre,
                Especialidad = VacioANull(especialidad),
                Telefono = VacioANull(telefono),
                Correo = VacioANull(correo),
                Direccion = VacioANull(direccion),
                NumeroJVPO = VacioANull(jvpo),
                Activo = DoctorSeleccionado?.Activo ?? true
            };

            return true;
        }

        private void CargarFormularioDesdeSeleccion()
        {
            if (DoctorSeleccionado == null)
            {
                LimpiarFormulario();
                return;
            }

            NombreCompleto = DoctorSeleccionado.NombreCompleto;
            Especialidad = DoctorSeleccionado.Especialidad ?? string.Empty;
            Telefono = DoctorSeleccionado.Telefono ?? string.Empty;
            Correo = DoctorSeleccionado.Correo ?? string.Empty;
            Direccion = DoctorSeleccionado.Direccion ?? string.Empty;
            NumeroJVPO = DoctorSeleccionado.NumeroJVPO ?? string.Empty;
        }

        private void PrepararNuevo()
        {
            DoctorSeleccionado = null;
            LimpiarFormulario();
            LimpiarMensaje();
        }

        private void LimpiarFormulario()
        {
            NombreCompleto = string.Empty;
            Especialidad = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Direccion = string.Empty;
            NumeroJVPO = string.Empty;
        }

        private async Task RegistrarAuditoriaSeguraAsync(string accion, string detalle)
        {
            try
            {
                await _bitacoraRepo.RegistrarMovimientoAsync(
                    UsuarioActual.NombreUsuario,
                    accion,
                    "Personal.Doctores",
                    detalle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo registrar la auditoría de doctores: {ex.Message}");
            }
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(TotalDoctores));
            OnPropertyChanged(nameof(TotalActivos));
            OnPropertyChanged(nameof(TotalInactivos));
        }

        private static string? VacioANull(string valor) => string.IsNullOrWhiteSpace(valor) ? null : valor;
    }
}
