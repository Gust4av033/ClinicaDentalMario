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
using ClinicaDentalMario.Views.Reportes;
using ClinicaDentalMario.Views.Tratamientos;

namespace ClinicaDentalMario.Navigation
{
    public sealed class ShellViewFactory : IShellViewFactory
    {
        public object CrearDashboard(Action<object> navegar)
        {
            return new DashboardView
            {
                DataContext = new DashboardViewModel(navegar)
            };
        }

        public object CrearPacientes(Action<object> navegar)
        {
            return new ListaPacientesView
            {
                DataContext = new ListaPacientesViewModel(navegar)
            };
        }

        public object CrearAgenda(Action<object> navegar)
        {
            return new AgendaView
            {
                DataContext = new AgendaViewModel(navegar)
            };
        }

        public object CrearTratamientos(Action<object> navegar)
        {
            return new TratamientosView
            {
                DataContext = new TratamientosViewModel(navegar)
            };
        }

        public object CrearPagos(Action<object> navegar)
        {
            return new EstadoCuentaView
            {
                DataContext = new EstadoCuentaViewModel(navegar)
            };
        }

        public object CrearReportes(Action<object> navegar)
        {
            return new ReportesView
            {
                DataContext = new ReportesViewModel(navegar)
            };
        }

        public object CrearConfiguracion()
        {
            return new ConfiguracionView
            {
                DataContext = new ConfiguracionViewModel()
            };
        }
    }
}
