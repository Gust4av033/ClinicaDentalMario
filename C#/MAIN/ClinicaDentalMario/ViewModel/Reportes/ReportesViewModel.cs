using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Reportes; // Necesario para instanciar las vistas
using System;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReportesViewModel : ViewModelBase
    {
        // 🔥 CAMBIO AQUÍ: Cambiamos Action<ViewModelBase> por Action<object> 🔥
        private readonly Action<object> _navegar;

        public ICommand AbrirReporteIngresosCommand { get; }
        public ICommand AbrirReportePacientesCommand { get; }
        public ICommand AbrirReporteTratamientosCommand { get; }
        public ICommand AbrirReporteAgendaCommand { get; }
        public ICommand AbrirReporteMarketingCommand { get; }
        public ICommand AbrirReporteCatálogoCommand { get; }

        // 🔥 CAMBIO AQUÍ TAMBIÉN: En el parámetro del constructor 🔥
        public ReportesViewModel(Action<object> navegar)
        {
            _navegar = navegar;
            Titulo = "Centro de Reportes y Estadísticas";

            AbrirReporteIngresosCommand = new RelayCommand(AbrirIngresos);
            AbrirReportePacientesCommand = new RelayCommand(p => { /* Próximamente */ });
            AbrirReporteTratamientosCommand = new RelayCommand(p =>
            {
                var vista = new ReporteProductividadView();
                vista.DataContext = new ReporteProductividadViewModel(_navegar);
                _navegar(vista);
            });
            AbrirReporteAgendaCommand = new RelayCommand(p => { /* Próximamente */ });
            AbrirReporteMarketingCommand = new RelayCommand(p => { /* Próximamente */ });
            AbrirReporteCatálogoCommand = new RelayCommand(p => { /* Próximamente */ });
        }

        private void AbrirIngresos(object? parameter) 
        { 
            // 1. Creamos la vista física
            var vista = new ReporteIngresosView();

            // 2. Creamos su cerebro
            var viewModel = new ReporteIngresosViewModel(_navegar);
            // 3. Los conectamos
            vista.DataContext = viewModel;
            
            // 4. Le decimos al MainWindow que pinte esta vista
            _navegar(vista); 
        }

    }
}