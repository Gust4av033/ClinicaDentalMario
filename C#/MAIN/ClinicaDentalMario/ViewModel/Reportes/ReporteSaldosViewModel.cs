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
    public sealed class ReporteSaldosViewModel : ViewModelBase
    {
        private readonly Action<object> _navegar;
        private readonly ReporteRepository _reporteRepository;
        private readonly ReportePdfService _pdfService;

        private ObservableCollection<ReporteSaldoPacienteModel> _listaSaldos = new();
        public ObservableCollection<ReporteSaldoPacienteModel> ListaSaldos
        {
            get => _listaSaldos;
            private set
            {
                if (SetProperty(ref _listaSaldos, value))
                    ActualizarResumen();
            }
        }

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set => SetProperty(ref _terminoBusqueda, value);
        }

        private int _pacientesConSaldo;
        public int PacientesConSaldo { get => _pacientesConSaldo; private set => SetProperty(ref _pacientesConSaldo, value); }

        private decimal _totalCargos;
        public decimal TotalCargos { get => _totalCargos; private set => SetProperty(ref _totalCargos, value); }

        private decimal _totalPagado;
        public decimal TotalPagado { get => _totalPagado; private set => SetProperty(ref _totalPagado, value); }

        private decimal _saldoPendiente;
        public decimal SaldoPendiente { get => _saldoPendiente; private set => SetProperty(ref _saldoPendiente, value); }

        public AsyncRelayCommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }

        public ReporteSaldosViewModel(Action<object> navegar)
        {
            _navegar = navegar ?? throw new ArgumentNullException(nameof(navegar));
            _reporteRepository = new ReporteRepository();
            _pdfService = new ReportePdfService();

            Titulo = "Saldos Pendientes";

            GenerarReporteCommand = new AsyncRelayCommand(_ => CargarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, _ => ListaSaldos.Count > 0);
            VolverCommand = new RelayCommand(Volver);

            _ = CargarAsync();
        }

        private async Task CargarAsync()
        {
            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    var saldos = await _reporteRepository.ObtenerSaldosPacientesAsync(TerminoBusqueda);
                    ListaSaldos = new ObservableCollection<ReporteSaldoPacienteModel>(saldos);
                }
                catch (Exception ex)
                {
                    ListaSaldos = new ObservableCollection<ReporteSaldoPacienteModel>();
                    MessageBox.Show(
                        "No fue posible cargar los saldos pendientes.\n\n" + ex.Message,
                        "Reportes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });
        }

        private void ActualizarResumen()
        {
            PacientesConSaldo = ListaSaldos.Count;
            TotalCargos = ListaSaldos.Sum(x => x.TotalCargos);
            TotalPagado = ListaSaldos.Sum(x => x.TotalPagado);
            SaldoPendiente = ListaSaldos.Sum(x => x.SaldoPendiente);
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaSaldos.Count == 0)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"SaldosPendientes_{DateTime.Today:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "Documentos PDF (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _pdfService.GenerarSaldosPdf(ListaSaldos, dialog.FileName);
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
