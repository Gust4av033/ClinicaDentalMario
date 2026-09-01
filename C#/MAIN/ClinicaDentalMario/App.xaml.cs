using ClinicaDentalMario.Data;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Login;
using ClinicaDentalMario.Views.Login;
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

                LoginView loginWindow = new LoginView
                {
                    DataContext = new LoginViewModel(
                        new UsuarioRepository(),
                        _exceptionHandler)
                };

                Current.MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                _exceptionHandler.Manejar(
                    ex,
                    "No fue posible inicializar la aplicación.");

                Current.Shutdown();
            }
        }

        private void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            _exceptionHandler.Manejar(
                e.Exception,
                "Ocurrió un error inesperado en la aplicación.");

            e.Handled = true;
            Current.Shutdown();
        }
    }
}
