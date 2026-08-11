using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Views.Configuracion;
using ClinicaDentalMario.ViewModel.Configuracion;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Dashboard
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly DashboardRepository _dashboardRepo;
        private readonly Action<object> _cambiarVista;

        // --- MÉTRICAS ---
        private decimal _ingresosHoy;
        public decimal IngresosHoy { get => _ingresosHoy; set => SetProperty(ref _ingresosHoy, value); }

        private int _citasHoy;
        public int CitasHoy { get => _citasHoy; set => SetProperty(ref _citasHoy, value); }

        // --- LISTAS REALES ---
        public ObservableCollection<dynamic> ListaCitasHoy { get; set; } = new();
        public ObservableCollection<dynamic> Morosos { get; set; } = new();
        public ObservableCollection<dynamic> Cumpleaneros { get; set; } = new();

        public ICommand AbrirConfiguracionCommand { get; }

        public DashboardViewModel(Action<object> cambiarVista)
        {
            Titulo = "Panel Principal (Dashboard)";
            _dashboardRepo = new DashboardRepository();
            _cambiarVista = cambiarVista;

            AbrirConfiguracionCommand = new RelayCommand(AbrirConfiguracion);

            // Arrancamos la carga real de la base de datos
            _ = CargarDatosDashboardAsync();
        }

        public async Task CargarDatosDashboardAsync()
        {
            EstaCargando = true;
            try
            {
                DateTime hoy = DateTime.Today;

                // 1. Cargar Tarjetas (Las que ya tenías)
                IngresosHoy = await _dashboardRepo.ObtenerIngresosDelDiaAsync(hoy);
                CitasHoy = await _dashboardRepo.ObtenerTotalCitasHoyAsync(hoy);

                // 2. Cargar Listas (Las nuevas)
                var citas = await _dashboardRepo.ObtenerCitasHoyListaAsync();
                var morosos = await _dashboardRepo.ObtenerMorososAsync();
                var cumpleaneros = await _dashboardRepo.ObtenerCumpleanerosMesAsync();

                // Llenamos las listas visuales vaciándolas primero (por si se recarga)
                ListaCitasHoy.Clear();
                foreach (var item in citas) ListaCitasHoy.Add(item);

                Morosos.Clear();
                foreach (var item in morosos) Morosos.Add(item);

                Cumpleaneros.Clear();
                foreach (var item in cumpleaneros) Cumpleaneros.Add(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al cargar datos reales del dashboard: " + ex.Message);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void AbrirConfiguracion(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaConfig = new ConfiguracionView();
                vistaConfig.DataContext = new ConfiguracionViewModel();
                _cambiarVista(vistaConfig);
            }
        }
    }
}