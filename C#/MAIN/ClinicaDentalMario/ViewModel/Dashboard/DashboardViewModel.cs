using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.ViewModel.Configuracion;
using ClinicaDentalMario.Views.Configuracion;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        private int _tratamientosActivos;
        public int TratamientosActivos { get => _tratamientosActivos; set => SetProperty(ref _tratamientosActivos, value); }

        // --- LISTAS ---
        public ObservableCollection<dynamic> ListaCitasHoy { get; set; } = new();
        public ObservableCollection<dynamic> TopMorosos { get; set; } = new();

        public ICommand AbrirConfiguracionCommand { get; }

        public DashboardViewModel(Action<object> cambiarVista)
        {
            Titulo = "Resumen Ejecutivo";
            _dashboardRepo = new DashboardRepository();
            _cambiarVista = cambiarVista;

            AbrirConfiguracionCommand = new RelayCommand(AbrirConfiguracion);

            _ = CargarDatosDashboardAsync();
        }

        public async Task CargarDatosDashboardAsync()
        {
            EstaCargando = true;
            try
            {
                DateTime hoy = DateTime.Today;

                // 1. Cargar Tarjetas
                IngresosHoy = await _dashboardRepo.ObtenerIngresosDelDiaAsync(hoy);
                CitasHoy = await _dashboardRepo.ObtenerTotalCitasHoyAsync(hoy);

                // (Opcional: Si no tienes este método en tu repo, puedes ponerle un número estático por ahora o crearlo luego)
                // TratamientosActivos = await _dashboardRepo.ObtenerTratamientosActivosTotalesAsync();
                TratamientosActivos = 12; // Número de ejemplo mientras creas el método en Dapper

                // 2. Cargar Listas
                var citas = await _dashboardRepo.ObtenerCitasHoyListaAsync();
                var morososCompletos = await _dashboardRepo.ObtenerMorososAsync();

                // Llenamos la agenda
                ListaCitasHoy.Clear();
                foreach (var item in citas) ListaCitasHoy.Add(item);

                // Llenamos SOLO LOS TOP 5 MOROSOS para no romper el diseño
                TopMorosos.Clear();
                var top5 = morososCompletos.OrderByDescending(m => m.Saldo).Take(5).ToList();
                foreach (var item in top5) TopMorosos.Add(item);
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