using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class ConfiguracionViewModel : ViewModelBase
    {
        private readonly CatalogoRepository _catalogoRepo;
        private readonly IPermissionService _permissionService;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private ObservableCollection<CatalogoTratamientosModel> _listaTratamientos = new();
        public ObservableCollection<CatalogoTratamientosModel> ListaTratamientos
        {
            get => _listaTratamientos;
            private set
            {
                if (SetProperty(ref _listaTratamientos, value))
                {
                    OnPropertyChanged(nameof(CantidadTratamientos));
                    OnPropertyChanged(nameof(SinTratamientos));
                }
            }
        }

        private CatalogoTratamientosModel? _tratamientoSeleccionado;
        public CatalogoTratamientosModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set
            {
                if (SetProperty(ref _tratamientoSeleccionado, value))
                {
                    CargarFormularioDesdeSeleccion();
                    OnPropertyChanged(nameof(EsEdicionTratamiento));
                    OnPropertyChanged(nameof(TituloFormularioTratamiento));
                    OnPropertyChanged(nameof(TextoBotonGuardar));
                    DesactivarTratamientoCommand.NotificarCanExecuteChanged();
                }
            }
        }

        private string _nombreTratamiento = string.Empty;
        public string NombreTratamiento
        {
            get => _nombreTratamiento;
            set => SetProperty(ref _nombreTratamiento, value ?? string.Empty);
        }

        private string _descripcionTratamiento = string.Empty;
        public string DescripcionTratamiento
        {
            get => _descripcionTratamiento;
            set => SetProperty(ref _descripcionTratamiento, value ?? string.Empty);
        }

        private string _precioBaseTexto = string.Empty;
        public string PrecioBaseTexto
        {
            get => _precioBaseTexto;
            set => SetProperty(ref _precioBaseTexto, value ?? string.Empty);
        }

        private string _duracionMinutosTexto = "30";
        public string DuracionMinutosTexto
        {
            get => _duracionMinutosTexto;
            set => SetProperty(ref _duracionMinutosTexto, value ?? string.Empty);
        }

        public int CantidadTratamientos => ListaTratamientos.Count;
        public bool SinTratamientos => !EstaCargando && ListaTratamientos.Count == 0;
        public bool EsEdicionTratamiento => TratamientoSeleccionado != null;
        public string TituloFormularioTratamiento => EsEdicionTratamiento
            ? "Editar tratamiento seleccionado"
            : "Agregar tratamiento al catálogo";
        public string TextoBotonGuardar => EsEdicionTratamiento ? "Guardar cambios" : "Agregar tratamiento";

        public AsyncRelayCommand GuardarTratamientoCommand { get; }
        public AsyncRelayCommand DesactivarTratamientoCommand { get; }
        public AsyncRelayCommand RecargarTratamientosCommand { get; }
        public RelayCommand NuevoTratamientoCommand { get; }

        public ConfiguracionViewModel()
            : this(
                new CatalogoRepository(),
                new PermissionService(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public ConfiguracionViewModel(
            CatalogoRepository catalogoRepo,
            IPermissionService permissionService,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            Titulo = "Configuración del Sistema";
            _catalogoRepo = catalogoRepo ?? throw new ArgumentNullException(nameof(catalogoRepo));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
                throw new UnauthorizedAccessException("El usuario actual no puede acceder a Configuración.");

            GuardarTratamientoCommand = new AsyncRelayCommand(_ => GuardarTratamientoAsync());
            DesactivarTratamientoCommand = new AsyncRelayCommand(
                _ => DesactivarTratamientoAsync(),
                _ => TratamientoSeleccionado != null);
            RecargarTratamientosCommand = new AsyncRelayCommand(_ => CargarTratamientosAsync());
            NuevoTratamientoCommand = new RelayCommand(_ => PrepararNuevoTratamiento());

            _ = CargarTratamientosAsync();
        }

        private async Task CargarTratamientosAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion) || EstaCargando)
                return;

            int? idSeleccionado = TratamientoSeleccionado?.IdTratamiento;
            EstaCargando = true;
            LimpiarMensaje();
            OnPropertyChanged(nameof(SinTratamientos));

            try
            {
                var tratamientos = await _catalogoRepo.ObtenerTratamientosActivosAsync();
                ListaTratamientos = new ObservableCollection<CatalogoTratamientosModel>(tratamientos);

                if (idSeleccionado.HasValue)
                    TratamientoSeleccionado = ListaTratamientos.FirstOrDefault(x => x.IdTratamiento == idSeleccionado.Value);
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "cargar el catálogo de tratamientos"));
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinTratamientos));
            }
        }

        private async Task GuardarTratamientoAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                _messageService.MostrarAdvertencia(
                    "Solo los administradores pueden modificar el catálogo de tratamientos.",
                    "Acceso denegado");
                return;
            }

            if (!TryConstruirTratamiento(out CatalogoTratamientosModel tratamiento))
                return;

            if (EstaCargando)
                return;

            EstaCargando = true;
            LimpiarMensaje();

            try
            {
                int? idExcluir = TratamientoSeleccionado?.IdTratamiento;
                if (await _catalogoRepo.ExisteNombreTratamientoAsync(tratamiento.Nombre, idExcluir))
                {
                    MostrarAdvertencia("Ya existe un tratamiento activo con ese nombre.");
                    return;
                }

                if (TratamientoSeleccionado == null)
                {
                    await _catalogoRepo.InsertarTratamientoAsync(tratamiento);
                    MostrarExito("Tratamiento agregado al catálogo correctamente.");
                }
                else
                {
                    tratamiento.IdTratamiento = TratamientoSeleccionado.IdTratamiento;
                    await _catalogoRepo.ActualizarTratamientoAsync(tratamiento);
                    MostrarExito("Tratamiento actualizado correctamente.");
                }
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "guardar el tratamiento del catálogo"));
                return;
            }
            finally
            {
                EstaCargando = false;
            }

            PrepararNuevoTratamiento(mantenerMensaje: true);
            await CargarTratamientosAsync();
        }

        private async Task DesactivarTratamientoAsync()
        {
            if (TratamientoSeleccionado == null)
                return;

            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarConfiguracion))
            {
                _messageService.MostrarAdvertencia(
                    "No tienes permisos para modificar el catálogo.",
                    "Acceso denegado");
                return;
            }

            string nombre = TratamientoSeleccionado.Nombre;
            bool confirmar = _messageService.Confirmar(
                $"¿Deseas desactivar '{nombre}'?\n\nDejará de aparecer para tratamientos nuevos, pero los historiales existentes conservarán su referencia.",
                "Desactivar tratamiento");

            if (!confirmar)
                return;

            try
            {
                await _catalogoRepo.EliminarTratamientoAsync(TratamientoSeleccionado.IdTratamiento);
                PrepararNuevoTratamiento();
                await CargarTratamientosAsync();
                MostrarExito($"'{nombre}' fue desactivado del catálogo.");
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "desactivar el tratamiento"));
            }
        }

        private bool TryConstruirTratamiento(out CatalogoTratamientosModel tratamiento)
        {
            tratamiento = new CatalogoTratamientosModel();
            string nombre = NombreTratamiento.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MostrarAdvertencia("El nombre del tratamiento es obligatorio.");
                return false;
            }

            if (!TryParseDecimal(PrecioBaseTexto, out decimal precio) || precio < 0)
            {
                MostrarAdvertencia("Ingresa un precio base válido, mayor o igual a cero.");
                return false;
            }

            if (!int.TryParse(DuracionMinutosTexto.Trim(), out int duracion) || duracion < 5 || duracion > 480)
            {
                MostrarAdvertencia("La duración debe estar entre 5 y 480 minutos.");
                return false;
            }

            tratamiento = new CatalogoTratamientosModel
            {
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(DescripcionTratamiento)
                    ? null
                    : DescripcionTratamiento.Trim(),
                PrecioBase = precio,
                DuracionMinutos = duracion,
                Activo = true
            };

            return true;
        }

        private void CargarFormularioDesdeSeleccion()
        {
            if (TratamientoSeleccionado == null)
            {
                LimpiarFormulario();
                return;
            }

            NombreTratamiento = TratamientoSeleccionado.Nombre;
            DescripcionTratamiento = TratamientoSeleccionado.Descripcion ?? string.Empty;
            PrecioBaseTexto = TratamientoSeleccionado.PrecioBase.ToString("0.00", CultureInfo.CurrentCulture);
            DuracionMinutosTexto = (TratamientoSeleccionado.DuracionMinutos ?? 30).ToString(CultureInfo.CurrentCulture);
        }

        private void PrepararNuevoTratamiento(bool mantenerMensaje = false)
        {
            TratamientoSeleccionado = null;
            LimpiarFormulario();

            if (!mantenerMensaje)
                LimpiarMensaje();
        }

        private void LimpiarFormulario()
        {
            NombreTratamiento = string.Empty;
            DescripcionTratamiento = string.Empty;
            PrecioBaseTexto = string.Empty;
            DuracionMinutosTexto = "30";
        }

        private static bool TryParseDecimal(string texto, out decimal valor)
        {
            return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out valor)
                || decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }
    }
}
