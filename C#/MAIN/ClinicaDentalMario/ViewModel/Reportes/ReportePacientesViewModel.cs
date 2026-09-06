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
    public class ReportePacientesViewModel : ViewModelBase
    {
        private readonly Action<object> _navegar;
        private readonly PacienteRepository _pacienteRepository;
        private readonly ReportePdfService _pdfService;

        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            private set
            {
                if (SetProperty(ref _listaPacientes, value))
                {
                    ActualizarResumen();
                }
            }
        }

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set => SetProperty(ref _terminoBusqueda, value);
        }

        private bool _incluirInactivos;
        public bool IncluirInactivos
        {
            get => _incluirInactivos;
            set => SetProperty(ref _incluirInactivos, value);
        }

        private int _totalPacientes;
        public int TotalPacientes { get => _totalPacientes; private set => SetProperty(ref _totalPacientes, value); }

        private int _menoresEdad;
        public int MenoresEdad { get => _menoresEdad; private set => SetProperty(ref _menoresEdad, value); }

        private int _registradosMes;
        public int RegistradosMes { get => _registradosMes; private set => SetProperty(ref _registradosMes, value); }

        private int _sinTelefono;
        public int SinTelefono { get => _sinTelefono; private set => SetProperty(ref _sinTelefono, value); }

        public AsyncRelayCommand GenerarReporteCommand { get; }
        public ICommand ExportarPdfCommand { get; }
        public ICommand VolverCommand { get; }

        public ReportePacientesViewModel(Action<object> navegar)
        {
            _navegar = navegar ?? throw new ArgumentNullException(nameof(navegar));
            _pacienteRepository = new PacienteRepository();
            _pdfService = new ReportePdfService();

            Titulo = "Reporte General de Pacientes";

            GenerarReporteCommand = new AsyncRelayCommand(_ => CargarAsync());
            ExportarPdfCommand = new RelayCommand(ExportarPdf, _ => ListaPacientes.Count > 0);
            VolverCommand = new RelayCommand(Volver);

            _ = CargarAsync();
        }

        private async Task CargarAsync()
        {
            await EjecutarConCargaAsync(async () =>
            {
                try
                {
                    string termino = TerminoBusqueda.Trim();
                    IEnumerable<PacienteModel> pacientes;

                    if (IncluirInactivos)
                    {
                        if (string.IsNullOrWhiteSpace(termino))
                        {
                            var activosTask = _pacienteRepository.ObtenerTodosAsync();
                            var inactivosTask = _pacienteRepository.ObtenerInactivosAsync();
                            await Task.WhenAll(activosTask, inactivosTask);
                            pacientes = activosTask.Result.Concat(inactivosTask.Result);
                        }
                        else
                        {
                            var activosTask = _pacienteRepository.BuscarAsync(termino);
                            var inactivosTask = _pacienteRepository.BuscarAsync(termino, soloInactivos: true);
                            await Task.WhenAll(activosTask, inactivosTask);
                            pacientes = activosTask.Result.Concat(inactivosTask.Result);
                        }
                    }
                    else
                    {
                        pacientes = string.IsNullOrWhiteSpace(termino)
                            ? await _pacienteRepository.ObtenerTodosAsync()
                            : await _pacienteRepository.BuscarAsync(termino);
                    }

                    ListaPacientes = new ObservableCollection<PacienteModel>(
                        pacientes.OrderBy(x => x.NombreCompleto));
                }
                catch (Exception ex)
                {
                    ListaPacientes = new ObservableCollection<PacienteModel>();
                    MessageBox.Show(
                        "No fue posible generar el reporte de pacientes.\n\n" + ex.Message,
                        "Reportes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            });
        }

        private void ActualizarResumen()
        {
            DateTime hoy = DateTime.Today;
            TotalPacientes = ListaPacientes.Count;
            MenoresEdad = ListaPacientes.Count(x => x.FechaNacimiento.HasValue && x.Edad < 18);
            RegistradosMes = ListaPacientes.Count(x =>
                x.FechaRegistro.Year == hoy.Year && x.FechaRegistro.Month == hoy.Month);
            SinTelefono = ListaPacientes.Count(x => string.IsNullOrWhiteSpace(x.Telefono));
        }

        private void ExportarPdf(object? parameter)
        {
            if (ListaPacientes.Count == 0)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = $"Pacientes_{DateTime.Today:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "Documentos PDF (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _pdfService.GenerarPacientesPdf(ListaPacientes, dialog.FileName, IncluirInactivos);
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
