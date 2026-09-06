using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Reportes;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReporteIngresosViewModel : ViewModelBase
    {
        private readonly PagoRepository _pagoRepo;
        private readonly Action<object> _navegar;

        private DateTime _fechaInicio = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime FechaInicio { get => _fechaInicio; set => SetProperty(ref _fechaInicio, value); }

        private DateTime _fechaFin = DateTime.Today;
        public DateTime FechaFin { get => _fechaFin; set => SetProperty(ref _fechaFin, value); }

        private decimal _totalIngresosPeriodo;
        public decimal TotalIngresosPeriodo { get => _totalIngresosPeriodo; private set => SetProperty(ref _totalIngresosPeriodo, value); }

        private int _totalPagos;
        public int TotalPagos { get => _totalPagos; private set => SetProperty(ref _totalPagos, value); }

        private decimal _promedioPago;
        public decimal PromedioPago { get => _promedioPago; private set => SetProperty(ref _promedioPago, value); }

        private string _metodoPrincipal = "---";
        public string MetodoPrincipal { get => _metodoPrincipal; private set => SetProperty(ref _metodoPrincipal, value); }

        private ObservableCollection<dynamic> _listaIngresos = new();
        public ObservableCollection<dynamic> ListaIngresos
        {
            get => _listaIngresos;
            private set => SetProperty(ref _listaIngresos, value);
        }

        public AsyncRelayCommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }

        public ReporteIngresosViewModel(Action<object> navegar)
        {
            _navegar = navegar ?? throw new ArgumentNullException(nameof(navegar));
            _pagoRepo = new PagoRepository();
            Titulo = "Ingresos / Corte de Caja";

            GenerarReporteCommand = new AsyncRelayCommand(_ => GenerarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, _ => ListaIngresos.Count > 0);
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
                    var ingresos = await _pagoRepo.ObtenerIngresosPorRangoAsync(FechaInicio.Date, FechaFin.Date);
                    ListaIngresos = new ObservableCollection<dynamic>(ingresos);
                    ActualizarResumen();
                }
                catch (Exception ex)
                {
                    ListaIngresos = new ObservableCollection<dynamic>();
                    ActualizarResumen();
                    MessageBox.Show("No fue posible generar el corte de caja.\n\n" + ex.Message, "Reportes", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void ActualizarResumen()
        {
            TotalPagos = ListaIngresos.Count;
            TotalIngresosPeriodo = ListaIngresos.Sum(x => Convert.ToDecimal(x.Monto));
            PromedioPago = TotalPagos == 0 ? 0m : TotalIngresosPeriodo / TotalPagos;

            MetodoPrincipal = ListaIngresos.Count == 0
                ? "---"
                : ListaIngresos
                    .GroupBy(x => string.IsNullOrWhiteSpace(Convert.ToString(x.MetodoPago)) ? "Sin especificar" : Convert.ToString(x.MetodoPago)!)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .First().Key;
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaIngresos.Count == 0)
                return;

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"CorteDeCaja_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}",
                    DefaultExt = ".pdf",
                    Filter = "Documentos PDF (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() != true)
                    return;

                var pdfService = new ClinicaDentalMario.Services.PdfService();
                pdfService.GenerarReporteIngresosPdf(
                    FechaInicio,
                    FechaFin,
                    ListaIngresos,
                    TotalIngresosPeriodo,
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
