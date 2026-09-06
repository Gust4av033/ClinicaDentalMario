using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Reportes;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReportesViewModel : ViewModelBase
    {
        private readonly Action<object> _navegar;

        public ICommand AbrirReporteIngresosCommand { get; }
        public ICommand AbrirReportePacientesCommand { get; }
        public ICommand AbrirReporteSaldosCommand { get; }
        public ICommand AbrirReporteTratamientosCommand { get; }
        public ICommand AbrirReporteAgendaCommand { get; }

        public ReportesViewModel(Action<object> navegar)
        {
            _navegar = navegar ?? throw new ArgumentNullException(nameof(navegar));
            Titulo = "Centro de Reportes";

            AbrirReporteIngresosCommand = new RelayCommand(AbrirIngresos);
            AbrirReportePacientesCommand = new RelayCommand(AbrirPacientes);
            AbrirReporteSaldosCommand = new RelayCommand(AbrirSaldos);
            AbrirReporteTratamientosCommand = new RelayCommand(AbrirProductividad);
            AbrirReporteAgendaCommand = new RelayCommand(AbrirAgenda);
        }

        private void AbrirIngresos(object? parameter)
        {
            NavegarA(new ReporteIngresosView(), new ReporteIngresosViewModel(_navegar));
        }

        private void AbrirPacientes(object? parameter)
        {
            NavegarA(new ReportePacientesView(), new ReportePacientesViewModel(_navegar));
        }

        private void AbrirSaldos(object? parameter)
        {
            NavegarA(new ReporteSaldosView(), new ReporteSaldosViewModel(_navegar));
        }

        private void AbrirProductividad(object? parameter)
        {
            NavegarA(new ReporteProductividadView(), new ReporteProductividadViewModel(_navegar));
        }

        private void AbrirAgenda(object? parameter)
        {
            NavegarA(new ReporteAgendaView(), new ReporteAgendaViewModel(_navegar));
        }

        private void NavegarA(object vista, object viewModel)
        {
            if (vista is System.Windows.FrameworkElement elemento)
            {
                elemento.DataContext = viewModel;
            }

            _navegar(vista);
        }
    }
}
