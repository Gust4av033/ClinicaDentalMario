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
    public class ReporteProductividadViewModel : ViewModelBase
    {
        private readonly TratamientoRepository _tratamientoRepo; // O el repo donde pusiste el método
        private readonly Action<object> _navegar;

        private DateTime _fechaInicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime FechaInicio { get => _fechaInicio; set => SetProperty(ref _fechaInicio, value); }

        private DateTime _fechaFin = DateTime.Today;
        public DateTime FechaFin { get => _fechaFin; set => SetProperty(ref _fechaFin, value); }

        private int _totalTratamientosPeriodo;
        public int TotalTratamientosPeriodo { get => _totalTratamientosPeriodo; set => SetProperty(ref _totalTratamientosPeriodo, value); }

        private ObservableCollection<dynamic> _listaProductividad = new();
        public ObservableCollection<dynamic> ListaProductividad { get => _listaProductividad; set => SetProperty(ref _listaProductividad, value); }

        public ICommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }

        public ReporteProductividadViewModel(Action<object> navegar)
        {
            _navegar = navegar;
            Titulo = "Productividad Clínica";
            _tratamientoRepo = new TratamientoRepository();

            GenerarReporteCommand = new RelayCommand(async (p) => await GenerarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, (p) => ListaProductividad.Any());
            VolverCommand = new RelayCommand(Volver);

            _ = GenerarAsync();
        }

        private async Task GenerarAsync()
        {
            if (FechaInicio > FechaFin) return;

            EstaCargando = true;
            try
            {
                var resultados = await _tratamientoRepo.ObtenerProductividadAsync(FechaInicio, FechaFin);
                ListaProductividad = new ObservableCollection<dynamic>(resultados);
                TotalTratamientosPeriodo = ListaProductividad.Sum(x => (int)x.Cantidad);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EstaCargando = false; }
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaProductividad == null || !ListaProductividad.Any())
            {
                MessageBox.Show("No hay datos para exportar en este periodo.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Abrimos la ventana de Windows para guardar el archivo
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Productividad_{FechaInicio:ddMMyy}_{FechaFin:ddMMyy}",
                    DefaultExt = ".pdf",
                    Filter = "Documentos PDF (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    EstaCargando = true;

                    // Invocamos el PdfService
                    var pdfService = new ClinicaDentalMario.Services.PdfService();
                    pdfService.GenerarReporteProductividadPdf(
                        FechaInicio,
                        FechaFin,
                        ListaProductividad,
                        TotalTratamientosPeriodo,
                        dialog.FileName
                    );

                    MessageBox.Show("¡Reporte generado y guardado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Abrir el PDF automáticamente para que el doctor lo vea de inmediato
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
        private void Volver(object? parameter)
        {
            var vista = new ReportesView();
            vista.DataContext = new ReportesViewModel(_navegar);
            _navegar(vista);
        }
    }
}