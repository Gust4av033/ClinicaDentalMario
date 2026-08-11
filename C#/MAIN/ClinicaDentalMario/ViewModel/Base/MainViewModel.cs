using ClinicaDentalMario.ViewModel.Agenda; // <-- Asegúrate de tener este using para la Agenda
using ClinicaDentalMario.ViewModel.Configuracion;
using ClinicaDentalMario.ViewModel.Dashboard;
using ClinicaDentalMario.ViewModel.Pacientes;
using ClinicaDentalMario.ViewModel.Pagos;
using ClinicaDentalMario.ViewModel.Tratamientos;
using ClinicaDentalMario.Views.Agenda; // <-- Asegúrate de tener este using para la vista de Agenda
using ClinicaDentalMario.Views.Configuracion;
using ClinicaDentalMario.Views.Dashboard;
using ClinicaDentalMario.Views.Pacientes;
using ClinicaDentalMario.Views.Pagos;
using ClinicaDentalMario.Views.Tratamientos;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Base
{
    public class MainViewModel : ViewModelBase
    {
        private object _vistaActual;
        public object VistaActual
        {
            get => _vistaActual;
            set => SetProperty(ref _vistaActual, value);
        }

        // Comandos de todo el menú lateral
        public ICommand NavegarDashboardCommand { get; }
        public ICommand NavegarPacientesCommand { get; }
        public ICommand NavegarAgendaCommand { get; }
        public ICommand NavegarTratamientosCommand { get; }
        public ICommand NavegarPagosCommand { get; }
        public ICommand NavegarOdontogramaCommand { get; }
        public ICommand NavegarReportesCommand { get; }
        public ICommand NavegarConfiguracionCommand { get; }


        public MainViewModel()
        {
            // Al abrir la app, cargamos el Dashboard por defecto
            CargarDashboard();

            // Vinculamos cada botón de tu MainWindow.xaml a su respectiva acción
            NavegarDashboardCommand = new RelayCommand(p => CargarDashboard());
            NavegarPacientesCommand = new RelayCommand(p => CargarPacientes());

            // 🔥 AQUÍ "PRENDEMOS" EL BOTÓN DE LA AGENDA 🔥
            NavegarAgendaCommand = new RelayCommand(p => CargarAgenda());

            // Los demás los dejamos prevenidos para que no den error al hacerles clic
            NavegarTratamientosCommand = new RelayCommand(p => { /* Próximamente */ });
            // En MainViewModel.cs:
            NavegarPagosCommand = new RelayCommand(p =>
            {
                var vista = new EstadoCuentaView();
                var viewModel = new EstadoCuentaViewModel(CambiarVista);
                vista.DataContext = viewModel;
                VistaActual = vista;
            }); NavegarOdontogramaCommand = new RelayCommand(p => { /* Próximamente */ });
            NavegarReportesCommand = new RelayCommand(p => { /* Próximamente */ });
            // 2. En el constructor lo inicializas:
            NavegarConfiguracionCommand = new RelayCommand(p =>
            {
                var vista = new ConfiguracionView();
                var viewModel = new ConfiguracionViewModel();
                vista.DataContext = viewModel;

                VistaActual = vista; // Cambia el ContentControl de la pantalla central
            }); NavegarTratamientosCommand = new RelayCommand(p => CargarTratamientos());
        }

        private void CargarDashboard()
        {
            DashboardView vista = new DashboardView();
            vista.DataContext = new DashboardViewModel(CambiarVista);
            VistaActual = vista;
        }

        private void CargarTratamientos()
        {
            var vista = new TratamientosView();
            var viewModel = new TratamientosViewModel(CambiarVista);
            vista.DataContext = viewModel;
            VistaActual = vista;
        }

        private void CargarPacientes()
        {
            var vista = new ListaPacientesView();
            // Le pasamos el método CambiarVista para que la lista de pacientes pueda navegar a otras vistas
            var viewModel = new ListaPacientesViewModel(CambiarVista);
            vista.DataContext = viewModel;
            VistaActual = vista;
        }

        // 🔥 EL NUEVO MÉTODO PARA CARGAR LA AGENDA 🔥
        private void CargarAgenda()
        {
            var vista = new AgendaView();
            // Le pasamos el método CambiarVista para que pueda abrir "Nueva Cita" y "Editar Cita"
            var viewModel = new AgendaViewModel(CambiarVista);
            vista.DataContext = viewModel;
            VistaActual = vista;
        }

        // 🔥 MÉTODO AUXILIAR PARA LA NAVEGACIÓN 🔥
        private void CambiarVista(object nuevaVista)
        {
            VistaActual = nuevaVista;
        }
    }
}