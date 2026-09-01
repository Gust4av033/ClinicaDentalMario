using ClinicaDentalMario.Common;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Login
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly UsuarioRepository _usuarioRepo;
        private readonly IExceptionHandler _exceptionHandler;

        private string _usuario = string.Empty;
        public string Usuario
        {
            get => _usuario;
            set => SetProperty(ref _usuario, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand AccederCommand { get; }
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

            AccederCommand = new AsyncRelayCommand(AccederAsync);
            SalirCommand = new RelayCommand(Salir);
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

            var passwordBox = ventanaLogin.FindName("txtPassword") as PasswordBox;
            string passwordPlana = passwordBox?.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(passwordPlana))
            {
                MensajeError = "⚠️ Ingresa tu usuario y contraseña.";
                return;
            }

            MensajeError = string.Empty;

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    string hashPassword = EncriptarSHA256(passwordPlana);
                    var usuarioBD = await _usuarioRepo.LoginAsync(Usuario.Trim(), hashPassword);

                    if (usuarioBD is null || !usuarioBD.Activo)
                    {
                        MensajeError = "❌ Usuario o contraseña incorrectos.";
                        return;
                    }

                    UsuarioActual.IniciarSesion(usuarioBD, usuarioBD.NombreRol);

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    ventanaLogin.Close();
                }
                catch (Exception ex)
                {
                    MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                        ex,
                        "No fue posible iniciar sesión.");
                }
            });
        }
    }
}
