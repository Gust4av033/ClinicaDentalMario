using ClinicaDentalMario.Data;
using ClinicaDentalMario.ViewModel.Login;
using ClinicaDentalMario.Views.Login;
using System.Windows;

namespace ClinicaDentalMario
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // PASO 1: Inicializa y crea la base de datos en LocalDB si no existe
                await DatabaseInitializer.InicializarBaseDeDatosAsync();

                // PASO 2: Lanzamos la pantalla de Login
                LoginView loginWindow = new LoginView();
                loginWindow.DataContext = new LoginViewModel();

                Current.MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al inicializar la base de datos: {ex.Message}",
                                "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
    }
}