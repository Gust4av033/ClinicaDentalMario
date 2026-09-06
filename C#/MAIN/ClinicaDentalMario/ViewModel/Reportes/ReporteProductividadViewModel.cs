using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Reportes;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReporteProductividadViewModel : ViewModelBase
    {
        private readonly TratamientoRepository _tratamientoRepo;
        private readonly Action<object> _navegar;

        private DateTime _fechaInicio = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime FechaInicio { get => _fechaInicio; set => SetProperty(ref _fechaInicio, value); }

        private DateTime _fechaFin = DateTime.Today;
        public DateTime FechaFin { get => _fechaFin; set => SetProperty(ref _fechaFin, value); }

        private int _totalTratamientosPeriodo;
        public int TotalTratamientosPeriodo { get => _totalTratamientosPeriodo; private set => SetProperty(ref _totalTratamientosPeriodo, value); }

        private decimal _totalProyectadoPeriodo;
        public decimal TotalProyectadoPeriodo { get => _totalProyectadoPeriodo; private set => SetProperty(ref _totalProyectadoPeriodo, value); }

        private int _tiposTratamiento;
        public int TiposTratamiento { get => _tiposTratamiento; private set => SetProperty(ref _tiposTratamiento, value); }

        private ObservableCollection<dynamic> _listaProductividad = new();
        public ObservableCollection<dynamic> ListaProductividad { get => _listaProductividad; private set => SetProperty(ref _listaProductividad, value); }

        public AsyncRelayCommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }

        public ReporteProductividadViewModel(Action<object> navegar)
        {
            _navegar = navegar ?? throw new ArgumentNullException(nameof(navegar));
            _tratamientoRepo = new TratamientoRepository();
            Titulo = "Productividad Clínica";

            GenerarReporteCommand = new AsyncRelayCommand(_ => GenerarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, _ => ListaProductividad.Count > 0);
            VolverCommand = new RelayCommand(Volver);

            _ = GenerarAsync();
        }

        private async Task GenerarAsync()
        {
            if (FechaInicio.Date > FechaFin.Date)
            {
                MessageBox.Show("La fecha inicial no puede ser mayor que la fecha final.", "Rango inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    var resultados = await _tratamientoRepo.ObtenerProductividadAsync(FechaInicio.Date, FechaFin.Date);
                    ListaProductividad = new ObservableCollection<dynamic>(resultados);
                    ActualizarResumen();
                }
                catch (Exception ex)
                {
                    ListaProductividad = new ObservableCollection<dynamic>();
                    ActualizarResumen();
                    MessageBox.Show("No fue posible generar el reporte de tratamientos.\n\n" + ex.Message, "Reportes", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void ActualizarResumen()
        {
            TotalTratamientosPeriodo = ListaProductividad.Sum(x => Convert.ToInt32(x.Cantidad));
            TotalProyectadoPeriodo = ListaProductividad.Sum(x => Convert.ToDecimal(x.IngresoProyectado));
            TiposTratamiento = ListaProductividad.Count;
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaProductividad.Count == 0)
                return;

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Productividad_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}",
                    DefaultExt = ".pdf",
                    Filter = "Documentos PDF (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() != true)
                    return;

                var pdfService = new ClinicaDentalMario.Services.PdfService();
                pdfService.GenerarReporteProductividadPdf(
                    FechaInicio,
                    FechaFin,
                    ListaProductividad,
                    TotalTratamientosPeriodo,
                    dialog.FileName);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No fue posible exportar el reporte.\n\n" + ex.Message, "Reportes", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Volver(object? parameter)
        {
            var vista = new ReportesView
            {
                DataContext = new ReportesViewModel(_navegar)
            };
            _navegar(vista);
        }
    }
}
