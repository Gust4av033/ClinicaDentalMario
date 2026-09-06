using ClinicaDentalMario.Common;
using ClinicaDentalMario.Data;
using ClinicaDentalMario.Navigation;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.ViewModel.Login;
using ClinicaDentalMario.Views;
using ClinicaDentalMario.Views.Login;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace ClinicaDentalMario
{
    public partial class App : Application
    {
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        public App()
        {
            _messageService = new MessageService();
            _exceptionHandler = new ExceptionHandler(_messageService);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await DatabaseInitializer.InicializarBaseDeDatosAsync();
                MostrarLogin();
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(
                    ex,
                    "No fue posible inicializar la aplicación.");

                Current.Shutdown();
            }
        }

        public void MostrarLogin()
        {
            var loginExistente = Current.Windows
                .OfType<LoginView>()
                .FirstOrDefault(x => x.IsVisible);

            if (loginExistente is not null)
            {
                Current.MainWindow = loginExistente;
                loginExistente.Activate();
                return;
            }

            LoginView loginWindow = new LoginView
            {
                DataContext = new LoginViewModel(
                    new UsuarioRepository(),
                    _exceptionHandler)
            };

            Current.MainWindow = loginWindow;
            loginWindow.Show();
        }

        public void MostrarVentanaPrincipal()
        {
            if (!UsuarioActual.EstaAutenticado)
            {
                _messageService.MostrarAdvertencia(
                    "Debes iniciar sesión antes de acceder al sistema.",
                    "Sesión requerida");
                MostrarLogin();
                return;
            }

            var ventanaExistente = Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault(x => x.IsVisible);

            if (ventanaExistente is not null)
            {
                Current.MainWindow = ventanaExistente;
                ventanaExistente.Activate();
                return;
            }

            var mainViewModel = new MainViewModel(
                new NavigationService(),
                new ShellViewFactory(),
                new PermissionService(),
                _messageService,
                _exceptionHandler);

            MainWindow mainWindow = new MainWindow(mainViewModel);
            Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            // Los errores de inicialización se manejan y cierran desde OnStartup.
            // Un error aislado de una vista no debe tumbar toda la aplicación.
            try
            {
                Debug.WriteLine($"[DispatcherUnhandledException] {e.Exception}");
                _exceptionHandler.Manejar(
                    e.Exception,
                    "Ocurrió un error inesperado en la interfaz.");
            }
            catch (Exception handlerException)
            {
                Debug.WriteLine($"[Error manejando excepción global] {handlerException}");
                MessageBox.Show(
                    "Ocurrió un error inesperado en la interfaz.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            e.Handled = true;
        }
    }
}
