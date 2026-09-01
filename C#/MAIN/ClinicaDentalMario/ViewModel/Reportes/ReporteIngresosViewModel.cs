using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Reportes;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public class ReporteIngresosViewModel : ViewModelBase
    {
        private readonly PagoRepository _pagoRepo;
        private readonly Action<object> _navegar;

        // RANGO DE FECHAS
        private DateTime _fechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); // Primer día del mes actual
        public DateTime FechaInicio
        {
            get => _fechaInicio;
            set => SetProperty(ref _fechaInicio, value);
        }

        private DateTime _fechaFin = DateTime.Today;
        public DateTime FechaFin
        {
            get => _fechaFin;
            set => SetProperty(ref _fechaFin, value);
        }

        // DATOS PARA LA PANTALLA
        private decimal _totalIngresosPeriodo;
        public decimal TotalIngresosPeriodo
        {
            get => _totalIngresosPeriodo;
            set => SetProperty(ref _totalIngresosPeriodo, value);
        }

        private ObservableCollection<dynamic> _listaIngresos = new();
        public ObservableCollection<dynamic> ListaIngresos
        {
            get => _listaIngresos;
            set => SetProperty(ref _listaIngresos, value);
        }

        public ICommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }


        public ReporteIngresosViewModel(Action<object> navegar) // 🔥 RECIBE LA NAVEGACIÓN
        {
            _navegar = navegar;
            Titulo = "Reporte de Ingresos (Corte de Caja)";
            _pagoRepo = new PagoRepository();

            GenerarReporteCommand = new RelayCommand(async (p) => await GenerarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, (p) => ListaIngresos.Any());
            VolverCommand = new RelayCommand(Volver);

            _ = GenerarAsync();
        }

        private void Volver(object? parameter)
        {
            // Instanciamos el panel de tarjetas y nos regresamos
            var vista = new ReportesView();
            vista.DataContext = new ReportesViewModel(_navegar);
            _navegar(vista);
        }

        private async Task GenerarAsync()
        {
            if (FechaInicio > FechaFin)
            {
                MessageBox.Show("La Fecha de Inicio no puede ser mayor que la Fecha de Fin.", "Rango Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EstaCargando = true;
            try
            {
                var ingresos = await _pagoRepo.ObtenerIngresosPorRangoAsync(FechaInicio, FechaFin);
                ListaIngresos = new ObservableCollection<dynamic>(ingresos);

                // Calculamos la suma total de todo lo recaudado en esas fechas
                TotalIngresosPeriodo = ListaIngresos.Sum(x => (decimal)x.Monto);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EstaCargando = false; }
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaIngresos == null || !ListaIngresos.Any())
            {
                MessageBox.Show("No hay datos para exportar en este periodo.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Abrimos la ventana de Windows para guardar el archivo
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"CorteDeCaja_{FechaInicio:ddMMyy}_{FechaFin:ddMMyy}",
                    DefaultExt = ".pdf",
                    Filter = "Documentos PDF (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    EstaCargando = true; // Para mostrar feedback visual (opcional si tienes spinner)

                    // Invocamos el PdfService
                    var pdfService = new ClinicaDentalMario.Services.PdfService();
                    pdfService.GenerarReporteIngresosPdf(
                        FechaInicio,
                        FechaFin,
                        ListaIngresos,
                        TotalIngresosPeriodo,
                        dialog.FileName
                    );

                    MessageBox.Show("¡Reporte generado y guardado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Opcional: Abrir el PDF automáticamente
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}