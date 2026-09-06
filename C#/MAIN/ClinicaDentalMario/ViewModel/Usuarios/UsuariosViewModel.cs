using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Usuarios;
using System.Collections.ObjectModel;
using System.Windows;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class UsuariosViewModel : ViewModelBase
    {
        private const string Todos = "Todos";
        private const string Activos = "Activos";
        private const string Inactivos = "Inactivos";

        private readonly UsuarioRepository _usuarioRepo;
        private readonly IPermissionService _permissionService;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private List<UsuarioModel> _todosUsuarios = new();

        private ObservableCollection<UsuarioModel> _usuarios = new();
        public ObservableCollection<UsuarioModel> Usuarios
        {
            get => _usuarios;
            private set
            {
                if (SetProperty(ref _usuarios, value))
                    OnPropertyChanged(nameof(SinUsuarios));
            }
        }

        public ObservableCollection<string> EstadosFiltro { get; } = new() { Todos, Activos, Inactivos };
        public ObservableCollection<string> RolesFiltro { get; } = new() { Todos };

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

        private string _rolSeleccionado = Todos;
        public string RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                if (SetProperty(ref _rolSeleccionado, value ?? Todos))
                    AplicarFiltros();
            }
        }

        public int TotalUsuarios => _todosUsuarios.Count;
        public int TotalActivos => _todosUsuarios.Count(x => x.Activo);
        public int TotalInactivos => _todosUsuarios.Count(x => !x.Activo);
        public int TotalAdministradores => _todosUsuarios.Count(x =>
            x.Activo && x.NombreRol.Equals(RolesSistema.Administrador, StringComparison.OrdinalIgnoreCase));
        public bool SinUsuarios => !EstaCargando && Usuarios.Count == 0;

        public AsyncRelayCommand CargarUsuariosCommand { get; }
        public RelayCommand NuevoUsuarioCommand { get; }
        public RelayCommand EditarUsuarioCommand { get; }
        public RelayCommand LimpiarFiltrosCommand { get; }

        public UsuariosViewModel()
            : this(
                new UsuarioRepository(),
                new PermissionService(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public UsuariosViewModel(
            UsuarioRepository usuarioRepo,
            IPermissionService permissionService,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            Titulo = "Gestión de Usuarios y Acceso";
            _usuarioRepo = usuarioRepo ?? throw new ArgumentNullException(nameof(usuarioRepo));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarUsuarios))
                throw new UnauthorizedAccessException("El usuario actual no puede administrar usuarios.");

            CargarUsuariosCommand = new AsyncRelayCommand(_ => CargarUsuariosAsync());
            NuevoUsuarioCommand = new RelayCommand(NuevoUsuario);
            EditarUsuarioCommand = new RelayCommand(EditarUsuario);
            LimpiarFiltrosCommand = new RelayCommand(_ => LimpiarFiltros());

            _ = CargarUsuariosAsync();
        }

        public async Task CargarUsuariosAsync()
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarUsuarios) || EstaCargando)
                return;

            EstaCargando = true;
            LimpiarMensaje();
            OnPropertyChanged(nameof(SinUsuarios));

            try
            {
                _todosUsuarios = (await _usuarioRepo.ListarUsuariosAsync()).ToList();
                ActualizarRolesFiltro();
                AplicarFiltros();
                NotificarResumen();
            }
            catch (Exception ex)
            {
                _todosUsuarios.Clear();
                Usuarios = new ObservableCollection<UsuarioModel>();
                MostrarError(_exceptionHandler.ObtenerMensajeUsuario(ex, "cargar los usuarios"));
                NotificarResumen();
            }
            finally
            {
                EstaCargando = false;
                OnPropertyChanged(nameof(SinUsuarios));
            }
        }

        private void AplicarFiltros()
        {
            IEnumerable<UsuarioModel> consulta = _todosUsuarios;
            string texto = TextoBusqueda.Trim();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                consulta = consulta.Where(x =>
                    x.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    x.NombreUsuario.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    (x.Correo?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (EstadoSeleccionado == Activos)
                consulta = consulta.Where(x => x.Activo);
            else if (EstadoSeleccionado == Inactivos)
                consulta = consulta.Where(x => !x.Activo);

            if (!string.IsNullOrWhiteSpace(RolSeleccionado) && RolSeleccionado != Todos)
            {
                consulta = consulta.Where(x =>
                    x.NombreRol.Equals(RolSeleccionado, StringComparison.OrdinalIgnoreCase));
            }

            Usuarios = new ObservableCollection<UsuarioModel>(consulta);
        }

        private void ActualizarRolesFiltro()
        {
            var roles = _todosUsuarios
                .Select(x => x.NombreRol)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            RolesFiltro.Clear();
            RolesFiltro.Add(Todos);
            foreach (string rol in roles)
                RolesFiltro.Add(rol);

            if (!RolesFiltro.Contains(RolSeleccionado))
                RolSeleccionado = Todos;
        }

        private void LimpiarFiltros()
        {
            TextoBusqueda = string.Empty;
            EstadoSeleccionado = Todos;
            RolSeleccionado = Todos;
            AplicarFiltros();
        }

        private void NuevoUsuario(object? parameter)
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarUsuarios))
            {
                _messageService.MostrarAdvertencia(
                    "Solo los administradores pueden registrar nuevos usuarios.",
                    "Acceso denegado");
                return;
            }

            AbrirEditor(null);
        }

        private void EditarUsuario(object? parameter)
        {
            if (!_permissionService.TienePermiso(PermisoSistema.AdministrarUsuarios))
            {
                _messageService.MostrarAdvertencia(
                    "No tienes permisos para modificar usuarios.",
                    "Acceso denegado");
                return;
            }

            if (parameter is UsuarioModel usuarioSeleccionado)
                AbrirEditor(usuarioSeleccionado);
        }

        private void AbrirEditor(UsuarioModel? usuario)
        {
            try
            {
                var vm = new NuevoEditarUsuarioViewModel(usuario);
                var modal = new NuevoEditarUsuarioWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                Window? owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
                if (owner != null)
                    modal.Owner = owner;

                if (modal.ShowDialog() == true && vm.UsuarioGuardado)
                    _ = CargarUsuariosAsync();
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(ex, "abrir el editor de usuarios");
            }
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(TotalUsuarios));
            OnPropertyChanged(nameof(TotalActivos));
            OnPropertyChanged(nameof(TotalInactivos));
            OnPropertyChanged(nameof(TotalAdministradores));
        }
    }
}
