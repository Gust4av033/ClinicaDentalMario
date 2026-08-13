using ClinicaDentalMario.Common;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Login
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly UsuarioRepository _usuarioRepo;

        private string _usuario = string.Empty;
        public string Usuario { get => _usuario; set => SetProperty(ref _usuario, value); }

        private string _mensajeError = string.Empty;
        public string MensajeError { get => _mensajeError; set => SetProperty(ref _mensajeError, value); }

        public ICommand AccederCommand { get; }
        public ICommand SalirCommand { get; } // 🔥 NUEVO COMANDO

        public LoginViewModel()
        {
            Titulo = "Iniciar Sesión - CDMario Dental";
            _usuarioRepo = new UsuarioRepository();

            AccederCommand = new RelayCommand(async (param) => await AccederAsync(param));
            SalirCommand = new RelayCommand(Salir); // 🔥 INICIALIZAR EL COMANDO
        }

        private void Salir(object? parameter)
        {
            Application.Current.Shutdown(); // Cierra el programa por completo
        }

        private string EncriptarSHA256(string textoTextoPlano)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoTextoPlano));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private async Task AccederAsync(object? parameter)
        {
            if (parameter is not Window ventanaLogin) return;

            var passwordBox = ventanaLogin.FindName("txtPassword") as PasswordBox;
            string passwordPlana = passwordBox?.Password ?? "";

            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(passwordPlana))
            {
                MensajeError = "⚠️ Ingresa tu usuario y contraseña.";
                return;
            }

            EstaCargando = true;
            MensajeError = string.Empty;

            try
            {
                string hashPassword = EncriptarSHA256(passwordPlana);
                var usuarioBD = await _usuarioRepo.LoginAsync(Usuario, hashPassword);

                if (usuarioBD != null && usuarioBD.Activo)
                {
                    //ROLSQLSERVER
                    UsuarioActual.IniciarSesion(usuarioBD, usuarioBD.NombreRol);

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    ventanaLogin.Close();
                }
                else
                {
                    MensajeError = "❌ Usuario o contraseña incorrectos.";
                }
            }
            catch (Exception ex)
            {
                MensajeError = "❌ Error de conexión: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}