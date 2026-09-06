using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;

namespace ClinicaDentalMario.ViewModel.Configuracion
{
    public class BitacoraViewModel : ViewModelBase
    {
        private const string Todos = "Todos";

        private readonly IPermissionService _permissionService;
        private readonly BitacoraRepository _bitacoraRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        private ObservableCollection<BitacoraModel> _registrosAuditoria = new();
        public ObservableCollection<BitacoraModel> RegistrosAuditoria
        {
            get => _registrosAuditoria;
            private set
            {
                if (SetProperty(ref _registrosAuditoria, value))
                {
                    OnPropertyChanged(nameof(SinResultados));
                    OnPropertyChanged(nameof(CantidadEnPagina));
                }
            }
        }

        public ObservableCollection<string> UsuariosFiltro { get; } = new() { Todos };
        public ObservableCollection<string> AccionesFiltro { get; } = new() { Todos };
        public ObservableCollection<string> TablasFiltro { get; } = new() { Todos };
        public ObservableCollection<int> TamanosPagina { get; } = new() { 25, 50, 100 };

        private string _textoBusqueda = string.Empty;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set => SetProperty(ref _textoBusqueda, value ?? string.Empty);
        }

        private string _usuarioSeleccionado = Todos;
        public string UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set => SetProperty(ref _usuarioSeleccionado, value ?? Todos);
        }

        private string _accionSeleccionada = Todos;
        public string AccionSeleccionada
        {
            get => _accionSeleccionada;
            set => SetProperty(ref _accionSeleccionada, value ?? Todos);
        }

        private string _tablaSeleccionada = Todos;
        public string TablaSeleccionada
        {
            get => _tablaSeleccionada;
            set => SetProperty(ref _tablaSeleccionada, value ?? Todos);
        }

        private DateTime? _fechaDesde = DateTime.Today.AddDays(-30);
        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set => SetProperty(ref _fechaDesde, value?.Date);
        }

        private DateTime? _fechaHasta = DateTime.Today;
        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set => SetProperty(ref _fechaHasta, value?.Date);
        }

        private BitacoraModel? _registroSeleccionado;
        public BitacoraModel? RegistroSeleccionado
        {
            get => _registroSeleccionado;
            set
            {
                if (SetProperty(ref _registroSeleccionado, value))
                    OnPropertyChanged(nameof(TieneRegistroSeleccionado));
            }
        }

        private int _paginaActual = 1;
        public int PaginaActual
        {
            get => _paginaActual;
            private set
            {
                if (SetProperty(ref _paginaActual, value))
                {
                    OnPropertyChanged(nameof(PaginaTexto));
                    NotificarPaginacion();
                }
            }
        }

        private int _totalPaginas = 1;
        public int TotalPaginas
        {
            get => _totalPaginas;
            private set
            {
                if (SetProperty(ref _totalPaginas, Math.Max(1, value)))
                {
                    OnPropertyChanged(nameof(PaginaTexto));
                    NotificarPaginacion();
                }
            }
        }

        private int _tamanoPagina = 50;
        public int TamanoPagina
        {
            get => _tamanoPagina;
            set
            {
                if (SetProperty(ref _tamanoPagina, value))
                    _ = CambiarTamanoPaginaAsync();
            }
        }

        private int _totalResultados;
        public int TotalResultados
        {
            get => _totalResultados;
            private set => SetProperty(ref _totalResultados, value);
        }

        private int _registrosHoy;
        public int RegistrosHoy
        {
            get => _registrosHoy;
            private set => SetProperty(ref _registrosHoy, value);
        }

        private int _accesosFallidos;
        public int AccesosFallidos
        {
            get => _accesosFallidos;
            private set => SetProperty(ref _accesosFallidos, value);
        }

        private string _ultimaActualizacion = "Sin actualizar";
        public string UltimaActualizacion
        {
            get => _ultimaActualizacion;
            private set => SetProperty(ref _ultimaActualizacion, value);
        }

        public bool SinResultados => !EstaCargando && RegistrosAuditoria.Count == 0;
        public bool TieneRegistroSeleccionado => RegistroSeleccionado != null;
        public int CantidadEnPagina => RegistrosAuditoria.Count;
        public string PaginaTexto => $"Página {PaginaActual} de {TotalPaginas}";

        public AsyncRelayCommand RecargarCommand { get; }
        public AsyncRelayCommand BuscarCommand { get; }
        public AsyncRelayCommand LimpiarFiltrosCommand { get; }
        public AsyncRelayCommand MostrarTodoCommand { get; }
        public AsyncRelayCommand PaginaAnteriorCommand { get; }
        public AsyncRelayCommand PaginaSiguienteCommand { get; }
        public RelayCommand CerrarDetalleCommand { get; }

        public BitacoraViewModel()
            : this(
                new PermissionService(),
                new BitacoraRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public BitacoraViewModel(
            IPermissionService permissionService,
            BitacoraRepository bitacoraRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            Titulo = "Auditoría y Bitácora del Sistema";
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _bitacoraRepository = bitacoraRepository ?? throw new ArgumentNullException(nameof(bitacoraRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            if (!_permissionService.TienePermiso(PermisoSistema.VerBitacora))
                throw new UnauthorizedAccessException("El usuario actual no puede consultar la bitácora.");

            RecargarCommand = new AsyncRelayCommand(_ => CargarAsync());
            BuscarCommand = new AsyncRelayCommand(_ => AplicarFiltrosAsync());
            LimpiarFiltrosCommand = new AsyncRelayCommand(_ => LimpiarFiltrosAsync());
            MostrarTodoCommand = new AsyncRelayCommand(_ => MostrarTodoAsync());
            PaginaAnteriorCommand = new AsyncRelayCommand(_ => IrPaginaAnteriorAsync(), _ => PaginaActual > 1);
            PaginaSiguienteCommand = new AsyncRelayCommand(_ => IrPaginaSiguienteAsync(), _ => PaginaActual < TotalPaginas);
            CerrarDetalleCommand = new RelayCommand(_ => RegistroSeleccionado = null);

            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            try
            {
                await CargarOpcionesFiltroAsync();
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "inicializar la bitácora"));
            }
        }

        private async Task CargarOpcionesFiltroAsync()
        {
            var opciones = await _bitacoraRepository.ObtenerOpcionesFiltroAsync();

            AgregarOpciones(UsuariosFiltro, opciones.Usuarios);
            AgregarOpciones(AccionesFiltro, opciones.Acciones);
            AgregarOpciones(TablasFiltro, opciones.Tablas);
        }

        private async Task AplicarFiltrosAsync()
        {
            if (!ValidarFechas())
                return;

            PaginaActual = 1;
            RegistroSeleccionado = null;
            await CargarAsync();
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            UsuarioSeleccionado = Todos;
            AccionSeleccionada = Todos;
            TablaSeleccionada = Todos;
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            PaginaActual = 1;
            RegistroSeleccionado = null;
            await CargarAsync();
        }

        private async Task MostrarTodoAsync()
        {
            TextoBusqueda = string.Empty;
            UsuarioSeleccionado = Todos;
            AccionSeleccionada = Todos;
            TablaSeleccionada = Todos;
            FechaDesde = null;
            FechaHasta = null;
            PaginaActual = 1;
            RegistroSeleccionado = null;
            await CargarAsync();
        }

        private async Task CambiarTamanoPaginaAsync()
        {
            PaginaActual = 1;
            await CargarAsync();
        }

        private async Task IrPaginaAnteriorAsync()
        {
            if (PaginaActual <= 1)
                return;

            PaginaActual--;
            RegistroSeleccionado = null;
            await CargarAsync();
        }

        private async Task IrPaginaSiguienteAsync()
        {
            if (PaginaActual >= TotalPaginas)
                return;

            PaginaActual++;
            RegistroSeleccionado = null;
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.VerBitacora))
            {
                _messageService.MostrarAdvertencia(
                    "Solo los administradores pueden consultar la bitácora del sistema.",
                    "Acceso denegado");
                return;
            }

            if (!ValidarFechas())
                return;

            if (EstaCargando)
                return;

            EstaCargando = true;
            LimpiarMensaje();
            OnPropertyChanged(nameof(SinResultados));

            try
            {
                var resumenTask = _bitacoraRepository.ObtenerResumenAsync(
                    TextoBusqueda,
                    UsuarioSeleccionado,
                    AccionSeleccionada,
                    TablaSeleccionada,
                    FechaDesde,
                    FechaHasta);

                var registrosTask = _bitacoraRepository.ListarMovimientosAsync(
                    TextoBusqueda,
                    UsuarioSeleccionado,
                    AccionSeleccionada,
                    TablaSeleccionada,
                    FechaDesde,
                    FechaHasta,
                    PaginaActual,
                    TamanoPagina);

                await Task.WhenAll(resumenTask, registrosTask);

                BitacoraResumenModel resumen = await resumenTask;
                var registros = await registrosTask;

                TotalResultados = resumen.Total;
                RegistrosHoy = resumen.Hoy;
                AccesosFallidos = resumen.AccesosFallidos;
                TotalPaginas = (int)Math.Ceiling(TotalResultados / (double)Math.Max(1, TamanoPagina));

                if (PaginaActual > TotalPaginas)
                    PaginaActual = TotalPaginas;

                RegistrosAuditoria = new ObservableCollection<BitacoraModel>(registros);
                UltimaActualizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                RegistrosAuditoria = new ObservableCollection<BitacoraModel>();
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "cargar la bitácora"));
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinResultados));
                NotificarPaginacion();
            }
        }

        private bool ValidarFechas()
        {
            if (FechaDesde.HasValue && FechaHasta.HasValue && FechaDesde.Value.Date > FechaHasta.Value.Date)
            {
                _messageService.MostrarAdvertencia(
                    "La fecha inicial no puede ser posterior a la fecha final.",
                    "Rango de fechas inválido");
                return false;
            }

            return true;
        }

        private static void AgregarOpciones(ObservableCollection<string> destino, IEnumerable<string> opciones)
        {
            foreach (string opcion in opciones.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!destino.Contains(opcion))
                    destino.Add(opcion);
            }
        }

        private void NotificarPaginacion()
        {
            PaginaAnteriorCommand?.NotificarCanExecuteChanged();
            PaginaSiguienteCommand?.NotificarCanExecuteChanged();
        }
    }
}
