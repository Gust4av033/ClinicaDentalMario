using ClinicaDentalMario.ViewModel.Base;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReportesViewModel : ViewModelBase
    {
        public ICommand AbrirReporteIngresosCommand { get; }
        public ICommand AbrirReportePacientesCommand { get; }
        public ICommand AbrirReporteTratamientosCommand { get; }

        public ReportesViewModel()
        {
            Titulo = "Centro de Reportes y Estadísticas";

            AbrirReporteIngresosCommand = new RelayCommand(AbrirIngresos);
            AbrirReportePacientesCommand = new RelayCommand(AbrirPacientes);
            AbrirReporteTratamientosCommand = new RelayCommand(AbrirTratamientos);
        }

        private void AbrirIngresos(object? parameter) { /* Navegar a ReporteIngresosViewModel */ }
        private void AbrirPacientes(object? parameter) { /* Navegar a ReportePacientesViewModel */ }
        private void AbrirTratamientos(object? parameter) { /* Navegar a ReporteTratamientosViewModel */ }
    }
}
