using ClinicaDentalMario.Common;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.Validators;
using ClinicaDentalMario.ViewModel.Base;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Login
{
    public class LoginViewModel : ValidatableViewModelBase
    {
        private readonly UsuarioRepository _usuarioRepo;
        private readonly IExceptionHandler _exceptionHandler;

        private string _usuario = string.Empty;
        public string Usuario
        {
            get => _usuario;
            set
            {
                if (SetProperty(ref _usuario, value))
                {
                    ValidarUsuario();
                    AccederCommand?.NotificarCanExecuteChanged();
                }
            }
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand AccederCommand { get; }
        public ICommand SalirCommand { get; }

        public LoginViewModel()
            : this(new UsuarioRepository(), new ExceptionHandler(new MessageService()))
        {
        }

        public LoginViewModel(UsuarioRepository usuarioRepo, IExceptionHandler exceptionHandler)
        {
            _usuarioRepo = usuarioRepo ?? throw new ArgumentNullException(nameof(usuarioRepo));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Iniciar Sesión - CDMario Dental";

            AccederCommand = new AsyncRelayCommand(AccederAsync, _ => PuedeAcceder());
            SalirCommand = new RelayCommand(Salir);
        }

        private bool PuedeAcceder()
        {
            return !string.IsNullOrWhiteSpace(Usuario) && !HasErrors;
        }

        private void ValidarUsuario()
        {
            var errores = ValidationRules
                .Requerido(Usuario, "El usuario")
                .Concat(ValidationRules.LongitudMaxima(Usuario, 50, "El usuario"));

            ValidarCampo(errores, nameof(Usuario));
        }

        private void Salir(object? parameter)
        {
            Application.Current.Shutdown();
        }

        private static string EncriptarSHA256(string textoPlano)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano));

            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        private async Task AccederAsync(object? parameter)
        {
            if (parameter is not Window ventanaLogin)
            {
                return;
            }

            ValidarUsuario();
            if (HasErrors)
            {
                MensajeError = "Revisa los datos ingresados.";
                return;
            }

            if (ventanaLogin.FindName("txtPassword") is not PasswordBox passwordBox)
            {
                MensajeError = "No fue posible leer la contraseña.";
                return;
            }

            string passwordPlana = passwordBox.Password;
            if (string.IsNullOrWhiteSpace(passwordPlana))
            {
                MensajeError = "Ingresa tu contraseña.";
                passwordBox.Focus();
                return;
            }

            MensajeError = string.Empty;

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    string usuarioNormalizado = Usuario.Trim();
                    string hashPassword = EncriptarSHA256(passwordPlana);
                    var usuarioBD = await _usuarioRepo.LoginAsync(usuarioNormalizado, hashPassword);

                    if (usuarioBD is null)
                    {
                        MensajeError = "Usuario o contraseña incorrectos.";
                        passwordBox.Clear();
                        passwordBox.Focus();
                        return;
                    }

                    if (!RolesSistema.EsRolReconocido(usuarioBD.NombreRol))
                    {
                        MensajeError = "La cuenta no tiene un rol válido asignado. Contacta al administrador.";
                        passwordBox.Clear();
                        return;
                    }

                    UsuarioActual.IniciarSesion(usuarioBD, usuarioBD.NombreRol);

                    if (Application.Current is not App app)
                    {
                        throw new InvalidOperationException("No fue posible acceder al contexto principal de la aplicación.");
                    }

                    app.MostrarVentanaPrincipal();
                    ventanaLogin.Close();
                }
                catch (Exception ex)
                {
                    UsuarioActual.CerrarSesion();
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible iniciar sesión.");
                }
            });
        }
    }
}
