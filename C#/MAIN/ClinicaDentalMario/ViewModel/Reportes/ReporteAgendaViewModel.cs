using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Reportes;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Reportes
{
    public sealed class ReporteAgendaViewModel : ViewModelBase
    {
        private readonly Action<object> _navegar;
        private readonly CitaRepository _citaRepository;
        private readonly ReportePdfService _pdfService;

        private DateTime _fechaInicio = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime FechaInicio { get => _fechaInicio; set => SetProperty(ref _fechaInicio, value); }

        private DateTime _fechaFin = DateTime.Today;
        public DateTime FechaFin { get => _fechaFin; set => SetProperty(ref _fechaFin, value); }

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda { get => _terminoBusqueda; set => SetProperty(ref _terminoBusqueda, value); }

        private ObservableCollection<AgendaCitaModel> _listaCitas = new();
        public ObservableCollection<AgendaCitaModel> ListaCitas
        {
            get => _listaCitas;
            private set
            {
                if (SetProperty(ref _listaCitas, value))
                    ActualizarResumen();
            }
        }

        private int _totalCitas;
        public int TotalCitas { get => _totalCitas; private set => SetProperty(ref _totalCitas, value); }

        private int _atendidas;
        public int Atendidas { get => _atendidas; private set => SetProperty(ref _atendidas, value); }

        private int _canceladas;
        public int Canceladas { get => _canceladas; private set => SetProperty(ref _canceladas, value); }

        private int _noAsistio;
        public int NoAsistio { get => _noAsistio; private set => SetProperty(ref _noAsistio, value); }

        public double TasaAsistencia
        {
            get
            {
                int citasEvaluables = Atendidas + NoAsistio;
                return citasEvaluables == 0
                    ? 0
                    : Math.Round((double)Atendidas / citasEvaluables * 100, 1);
            }
        }

        public AsyncRelayCommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }

        public ReporteAgendaViewModel(Action<object> navegar)
        {
            _navegar = navegar ?? throw new ArgumentNullException(nameof(navegar));
            _citaRepository = new CitaRepository();
            _pdfService = new ReportePdfService();

            Titulo = "Citas y Asistencia";

            GenerarReporteCommand = new AsyncRelayCommand(_ => CargarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, _ => ListaCitas.Count > 0);
            VolverCommand = new RelayCommand(Volver);

            _ = CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (FechaInicio.Date > FechaFin.Date)
            {
                MessageBox.Show(
                    "La fecha inicial no puede ser mayor que la fecha final.",
                    "Rango inválido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    var citas = await _citaRepository.ObtenerCitasAsync(
                        FechaInicio.Date,
                        FechaFin.Date.AddDays(1),
                        termino: TerminoBusqueda);

                    ListaCitas = new ObservableCollection<AgendaCitaModel>(citas);
                }
                catch (Exception ex)
                {
                    ListaCitas = new ObservableCollection<AgendaCitaModel>();
                    MessageBox.Show(
                        "No fue posible generar el reporte de citas.\n\n" + ex.Message,
                        "Reportes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });
        }

        private void ActualizarResumen()
        {
            TotalCitas = ListaCitas.Count;
            Atendidas = ListaCitas.Count(x => x.Estado.Equals("Atendida", StringComparison.OrdinalIgnoreCase));
            Canceladas = ListaCitas.Count(x => x.Estado.Equals("Cancelada", StringComparison.OrdinalIgnoreCase));
            NoAsistio = ListaCitas.Count(x => x.Estado.Equals("No Asistió", StringComparison.OrdinalIgnoreCase));
            OnPropertyChanged(nameof(TasaAsistencia));
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaCitas.Count == 0)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"Citas_{FechaInicio:yyyyMMdd}_{FechaFin:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "Documentos PDF (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _pdfService.GenerarAgendaPdf(ListaCitas, FechaInicio, FechaFin, dialog.FileName);
                AbrirArchivo(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No fue posible exportar el reporte.\n\n" + ex.Message,
                    "Reportes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        private static void AbrirArchivo(string ruta)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true
            });
        }
    }
}
