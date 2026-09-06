using ClinicaDentalMario.Common;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class NuevoEditarUsuarioViewModel : ViewModelBase
    {
        private readonly UsuarioRepository _usuarioRepo;
        private readonly bool _eraAdministrador;

        public bool EsEdicion { get; }
        public bool EsUsuarioActual => EsEdicion && UsuarioActual.Detalles?.IdUsuario == Usuario.IdUsuario;
        public bool PuedeCambiarEstado => !EsUsuarioActual;
        public string TextoAyudaPassword => EsEdicion
            ? "Déjala en blanco para conservar la contraseña actual."
            : "Mínimo 6 caracteres.";

        private UsuarioModel _usuario;
        public UsuarioModel Usuario
        {
            get => _usuario;
            set => SetProperty(ref _usuario, value);
        }

        private ObservableCollection<RolModel> _roles = new();
        public ObservableCollection<RolModel> Roles
        {
            get => _roles;
            private set => SetProperty(ref _roles, value);
        }

        private RolModel? _rolSeleccionado;
        public RolModel? RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                if (SetProperty(ref _rolSeleccionado, value) && value != null)
                    Usuario.IdRol = value.IdRol;
            }
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public bool UsuarioGuardado { get; private set; }

        public AsyncRelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }

        public NuevoEditarUsuarioViewModel(UsuarioModel? usuarioExistente = null)
        {
            _usuarioRepo = new UsuarioRepository();
            EsEdicion = usuarioExistente != null;
            _eraAdministrador = usuarioExistente?.NombreRol.Equals(
                RolesSistema.Administrador,
                StringComparison.OrdinalIgnoreCase) == true;

            if (EsEdicion)
            {
                Titulo = $"Editar usuario - {usuarioExistente!.NombreUsuario}";
                Usuario = new UsuarioModel
                {
                    IdUsuario = usuarioExistente.IdUsuario,
                    IdRol = usuarioExistente.IdRol,
                    NombreCompleto = usuarioExistente.NombreCompleto,
                    NombreUsuario = usuarioExistente.NombreUsuario,
                    Correo = usuarioExistente.Correo,
                    Activo = usuarioExistente.Activo,
                    NombreRol = usuarioExistente.NombreRol,
                    FechaCreacion = usuarioExistente.FechaCreacion
                };
            }
            else
            {
                Titulo = "Crear nuevo usuario";
                Usuario = new UsuarioModel { Activo = true };
            }

            GuardarCommand = new AsyncRelayCommand(GuardarAsync);
            CancelarCommand = new RelayCommand(Cancelar);

            _ = CargarRolesAsync();
        }

        private async Task CargarRolesAsync()
        {
            EstaCargando = true;
            MensajeError = string.Empty;

            try
            {
                Roles = new ObservableCollection<RolModel>(await _usuarioRepo.ListarRolesAsync());

                if (EsEdicion)
                    RolSeleccionado = Roles.FirstOrDefault(r => r.IdRol == Usuario.IdRol);
                else if (Roles.Count > 0)
                    RolSeleccionado = Roles[0];
            }
            catch (Exception ex)
            {
                MensajeError = "No se pudieron cargar los roles: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private static string EncriptarSHA256(string texto)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }

        private async Task GuardarAsync(object? parameter)
        {
            if (EstaCargando)
                return;

            MensajeError = string.Empty;
            NormalizarCampos();

            if (!ValidarCamposBasicos())
                return;

            Window? ventana = parameter as Window;
            PasswordBox? txtPassword = ventana?.FindName("txtPassword") as PasswordBox;
            string passwordIngresada = txtPassword?.Password ?? string.Empty;

            if (!ValidarPassword(passwordIngresada))
                return;

            EstaCargando = true;

            try
            {
                int? excluirId = EsEdicion ? Usuario.IdUsuario : null;
                if (await _usuarioRepo.ExisteNombreUsuarioAsync(Usuario.NombreUsuario, excluirId))
                {
                    MensajeError = "Ya existe otro usuario con ese nombre de inicio de sesión.";
                    return;
                }

                if (!await ValidarSeguridadAdministradoresAsync())
                    return;

                if (EsEdicion)
                {
                    await _usuarioRepo.ActualizarUsuarioAsync(Usuario);

                    if (!string.IsNullOrWhiteSpace(passwordIngresada))
                        await _usuarioRepo.CambiarPasswordAsync(Usuario.IdUsuario, EncriptarSHA256(passwordIngresada));
                }
                else
                {
                    Usuario.PasswordHash = EncriptarSHA256(passwordIngresada);
                    await _usuarioRepo.CrearUsuarioAsync(Usuario);
                }

                UsuarioGuardado = true;
                if (ventana != null)
                    ventana.DialogResult = true;
            }
            catch (Exception ex)
            {
                MensajeError = "No se pudo guardar el usuario: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void NormalizarCampos()
        {
            Usuario.NombreCompleto = Usuario.NombreCompleto.Trim();
            Usuario.NombreUsuario = Usuario.NombreUsuario.Trim();
            Usuario.Correo = string.IsNullOrWhiteSpace(Usuario.Correo)
                ? null
                : Usuario.Correo.Trim();
        }

        private bool ValidarCamposBasicos()
        {
            if (string.IsNullOrWhiteSpace(Usuario.NombreCompleto))
            {
                MensajeError = "El nombre completo es obligatorio.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Usuario.NombreUsuario))
            {
                MensajeError = "El nombre de usuario es obligatorio.";
                return false;
            }

            if (Usuario.NombreUsuario.Any(char.IsWhiteSpace))
            {
                MensajeError = "El nombre de usuario no debe contener espacios.";
                return false;
            }

            if (Usuario.Correo != null && !MailAddress.TryCreate(Usuario.Correo, out _))
            {
                MensajeError = "Ingresa un correo electrónico válido o deja el campo vacío.";
                return false;
            }

            if (RolSeleccionado == null)
            {
                MensajeError = "Debes seleccionar un rol.";
                return false;
            }

            if (EsUsuarioActual && !Usuario.Activo)
            {
                MensajeError = "No puedes desactivar el usuario con el que tienes la sesión iniciada.";
                return false;
            }

            if (EsUsuarioActual && !RolSeleccionado.Nombre.Equals(RolesSistema.Administrador, StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "No puedes retirar tu propio rol de Administrador durante la sesión actual.";
                return false;
            }

            return true;
        }

        private bool ValidarPassword(string password)
        {
            if (!EsEdicion && string.IsNullOrWhiteSpace(password))
            {
                MensajeError = "La contraseña es obligatoria para un usuario nuevo.";
                return false;
            }

            if (!string.IsNullOrEmpty(password) && password.Length < 6)
            {
                MensajeError = "La contraseña debe tener al menos 6 caracteres.";
                return false;
            }

            return true;
        }

        private async Task<bool> ValidarSeguridadAdministradoresAsync()
        {
            if (!EsEdicion || !_eraAdministrador)
                return true;

            bool seguiraAdministrador = Usuario.Activo &&
                RolSeleccionado?.Nombre.Equals(RolesSistema.Administrador, StringComparison.OrdinalIgnoreCase) == true;

            if (seguiraAdministrador)
                return true;

            int otrosAdministradores = await _usuarioRepo.ContarAdministradoresActivosAsync(Usuario.IdUsuario);
            if (otrosAdministradores > 0)
                return true;

            MensajeError = "No puedes desactivar o cambiar de rol al último Administrador activo del sistema.";
            return false;
        }

        private void Cancelar(object? parameter)
        {
            if (parameter is Window ventana)
                ventana.DialogResult = false;
        }
    }
}
