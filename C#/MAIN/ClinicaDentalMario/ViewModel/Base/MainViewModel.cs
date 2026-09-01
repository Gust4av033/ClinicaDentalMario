using ClinicaDentalMario.ViewModel.Agenda;
using ClinicaDentalMario.ViewModel.Configuracion;
using ClinicaDentalMario.ViewModel.Dashboard;
using ClinicaDentalMario.ViewModel.Pacientes;
using ClinicaDentalMario.ViewModel.Pagos;
using ClinicaDentalMario.ViewModel.Reportes;
using ClinicaDentalMario.ViewModel.Tratamientos;
using ClinicaDentalMario.Views.Agenda;
using ClinicaDentalMario.Views.Configuracion;
using ClinicaDentalMario.Views.Dashboard;
using ClinicaDentalMario.Views.Pacientes;
using ClinicaDentalMario.Views.Pagos;
using ClinicaDentalMario.Views.Reportes; // <-- IMPORTANTE
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
            CargarDashboard();

            NavegarDashboardCommand = new RelayCommand(p => CargarDashboard());
            NavegarPacientesCommand = new RelayCommand(p => CargarPacientes());
            NavegarAgendaCommand = new RelayCommand(p => CargarAgenda());
            NavegarTratamientosCommand = new RelayCommand(p => CargarTratamientos());

            NavegarPagosCommand = new RelayCommand(p =>
            {
                var vista = new EstadoCuentaView();
                var viewModel = new EstadoCuentaViewModel(CambiarVista);
                vista.DataContext = viewModel;
                VistaActual = vista;
            });

            NavegarOdontogramaCommand = new RelayCommand(p => { /* Próximamente */ });

            // 🔥 AQUÍ CONECTAMOS EL CENTRO DE REPORTES 🔥
            NavegarReportesCommand = new RelayCommand(p => CargarReportes());

            NavegarConfiguracionCommand = new RelayCommand(p =>
            {
                var vista = new ConfiguracionView();
                var viewModel = new ConfiguracionViewModel();
                vista.DataContext = viewModel;
                VistaActual = vista;
            });
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
            var viewModel = new ListaPacientesViewModel(CambiarVista);
            vista.DataContext = viewModel;
            VistaActual = vista;
        }

        private void CargarAgenda()
        {
            var vista = new AgendaView();
            var viewModel = new AgendaViewModel(CambiarVista);
            vista.DataContext = viewModel;
            VistaActual = vista;
        }

        // 🔥 EL MÉTODO QUE CREA LA VISTA DE REPORTES Y LA MUESTRA 🔥
        private void CargarReportes()
        {
            var vista = new ReportesView();
            var viewModel = new ReportesViewModel(CambiarVista); // Le pasamos CambiarVista
            vista.DataContext = viewModel;
            VistaActual = vista;
        }

        private void CambiarVista(object nuevaVista)
        {
            VistaActual = nuevaVista;
        }
    }
}