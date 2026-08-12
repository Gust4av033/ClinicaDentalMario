using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Login
{
    public partial class LoginViewModel : ViewModelBase
    {
        /* private string _usuario = string.Empty;
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

          // Declaración manual del comando
          public ICommand IniciarSesionCommand { get; }

          public LoginViewModel()
          {
              Titulo = "Iniciar Sesión";

              // Inicializamos el comando apuntando al método asíncrono
              IniciarSesionCommand = new RelayCommand(async (param) => await IniciarSesionAsync(param));
          }

          private async Task IniciarSesionAsync(object? passwordObj)
          {
              if (string.IsNullOrWhiteSpace(Usuario))
              {
                  MensajeError = "Por favor, ingresa tu usuario.";
                  return;
              }

              EstaCargando = true;
              MensajeError = string.Empty;

              // Simulación de espera a SQL Server
              await Task.Delay(1000);

              EstaCargando = false;
          }*/

        public ICommand AccederCommand { get; }

        public LoginViewModel()
        {
            Titulo = "Iniciar Sesión - CDMario Dental";
            AccederCommand = new RelayCommand(Acceder);
        }

        private void Acceder(object? parameter)
        {
            // 1. Instanciamos la ventana principal (MainWindow)
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // 2. Cerramos la ventana de login actual (el parameter viene desde el CommandParameter de la vista)
            if (parameter is System.Windows.Window ventanaLogin)
            {
                ventanaLogin.Close();
            }
        }
    }
}
