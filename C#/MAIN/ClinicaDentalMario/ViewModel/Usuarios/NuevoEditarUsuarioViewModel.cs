using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class NuevoEditarUsuarioViewModel : ViewModelBase
    {
        private readonly UsuarioRepository _usuarioRepo;
        public bool EsEdicion { get; }

        private UsuarioModel _usuario;
        public UsuarioModel Usuario { get => _usuario; set => SetProperty(ref _usuario, value); }

        private ObservableCollection<RolModel> _roles = new();
        public ObservableCollection<RolModel> Roles { get => _roles; set => SetProperty(ref _roles, value); }

        private RolModel? _rolSeleccionado;
        public RolModel? RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                if (SetProperty(ref _rolSeleccionado, value) && value != null)
                {
                    Usuario.IdRol = value.IdRol;
                }
            }
        }

        private string _mensajeError = "";
        public string MensajeError { get => _mensajeError; set => SetProperty(ref _mensajeError, value); }

        public bool UsuarioGuardado { get; private set; }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoEditarUsuarioViewModel(UsuarioModel? usuarioExistente = null)
        {
            _usuarioRepo = new UsuarioRepository();
            EsEdicion = usuarioExistente != null;

            if (EsEdicion)
            {
                Titulo = $"Editar Usuario - {usuarioExistente!.NombreUsuario}";
                Usuario = new UsuarioModel
                {
                    IdUsuario = usuarioExistente.IdUsuario,
                    IdRol = usuarioExistente.IdRol,
                    NombreCompleto = usuarioExistente.NombreCompleto,
                    NombreUsuario = usuarioExistente.NombreUsuario,
                    Correo = usuarioExistente.Correo,
                    Activo = usuarioExistente.Activo
                };
            }
            else
            {
                Titulo = "Crear Nuevo Usuario";
                Usuario = new UsuarioModel { Activo = true };
            }

            GuardarCommand = new RelayCommand(async p => await GuardarAsync(p));
            CancelarCommand = new RelayCommand(Cancelar);

            _ = CargarRolesAsync();
        }

        private async Task CargarRolesAsync()
        {
            try
            {
                var listaRoles = await _usuarioRepo.ListarRolesAsync();
                Roles = new ObservableCollection<RolModel>(listaRoles);

                if (EsEdicion)
                {
                    RolSeleccionado = System.Linq.Enumerable.FirstOrDefault(Roles, r => r.IdRol == Usuario.IdRol);
                }
                else if (Roles.Count > 0)
                {
                    RolSeleccionado = Roles[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar roles: " + ex.Message);
            }
        }

        private string EncriptarSHA256(string texto)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        private async Task GuardarAsync(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(Usuario.NombreCompleto) || string.IsNullOrWhiteSpace(Usuario.NombreUsuario))
            {
                MensajeError = "⚠️ El Nombre Completo y el Nombre de Usuario son obligatorios.";
                return;
            }

            if (RolSeleccionado == null)
            {
                MensajeError = "⚠️ Debes seleccionar un Rol.";
                return;
            }

            var ventana = parameter as Window;
            var txtPassword = ventana?.FindName("txtPassword") as System.Windows.Controls.PasswordBox;
            string passwordIngresada = txtPassword?.Password ?? "";

            if (!EsEdicion && string.IsNullOrWhiteSpace(passwordIngresada))
            {
                MensajeError = "⚠️ La contraseña es obligatoria para un usuario nuevo.";
                return;
            }

            EstaCargando = true;
            MensajeError = "";

            try
            {
                if (EsEdicion)
                {
                    await _usuarioRepo.ActualizarUsuarioAsync(Usuario);

                    // Si escribió una nueva contraseña en edición, también se la actualizamos
                    if (!string.IsNullOrWhiteSpace(passwordIngresada))
                    {
                        string nuevoHash = EncriptarSHA256(passwordIngresada);
                        await _usuarioRepo.CambiarPasswordAsync(Usuario.IdUsuario, nuevoHash);
                    }
                }
                else
                {
                    Usuario.PasswordHash = EncriptarSHA256(passwordIngresada);
                    await _usuarioRepo.CrearUsuarioAsync(Usuario);
                }

                UsuarioGuardado = true;
                if (ventana != null) ventana.DialogResult = true;
            }
            catch (Exception ex)
            {
                MensajeError = "❌ Error al guardar: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void Cancelar(object? parameter)
        {
            if (parameter is Window ventana) ventana.DialogResult = false;
        }
    }
}