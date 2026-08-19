using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.ViewModel.Configuracion;
using ClinicaDentalMario.Views.Configuracion;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace ClinicaDentalMario.ViewModel.Dashboard
{
    public class DashboardViewModel : ViewModelBase
    {
        // --- VARIABLES DE INTERFAZ (TIEMPO Y SALUDO) ---
        private string _saludoDinamico;
        public string SaludoDinamico { get => _saludoDinamico; set => SetProperty(ref _saludoDinamico, value); }

        private string _fechaHoraActual;
        public string FechaHoraActual { get => _fechaHoraActual; set => SetProperty(ref _fechaHoraActual, value); }

        private DispatcherTimer _timerReloj;

        // --- DEPENDENCIAS ---
        private readonly DashboardRepository _dashboardRepo;
        private readonly Action<object> _cambiarVista;

        // --- MÉTRICAS (TARJETAS) ---
        private decimal _ingresosHoy;
        public decimal IngresosHoy { get => _ingresosHoy; set => SetProperty(ref _ingresosHoy, value); }

        private int _citasHoy;
        public int CitasHoy { get => _citasHoy; set => SetProperty(ref _citasHoy, value); }

        private int _tratamientosActivos;
        public int TratamientosActivos { get => _tratamientosActivos; set => SetProperty(ref _tratamientosActivos, value); }

        // --- LISTAS ---
        public ObservableCollection<dynamic> ListaCitasHoy { get; set; } = new();
        public ObservableCollection<dynamic> TopMorosos { get; set; } = new();

        // --- COMANDOS ---
        public ICommand AbrirConfiguracionCommand { get; }

        public DashboardViewModel(Action<object> cambiarVista)
        {
            Titulo = "Resumen Ejecutivo";
            _dashboardRepo = new DashboardRepository();
            _cambiarVista = cambiarVista;

            AbrirConfiguracionCommand = new RelayCommand(AbrirConfiguracion);

            // 1. Iniciar el Reloj y el Saludo Dinámico
            IniciarRelojYSaludo();

            // 2. Cargar los datos desde SQL
            _ = CargarDatosDashboardAsync();
        }

        private void IniciarRelojYSaludo()
        {
            // Configurar Saludo Inicial
            ActualizarSaludo();

            // Configurar Reloj en tiempo real (Cada 1 segundo)
            _timerReloj = new DispatcherTimer();
            _timerReloj.Interval = TimeSpan.FromSeconds(1);
            _timerReloj.Tick += (s, e) =>
            {
                // Formato premium: "martes, 18 de agosto 2026 • 08:40:00 PM"
                FechaHoraActual = DateTime.Now.ToString("dddd, dd 'de' MMMM yyyy  •  hh:mm:ss tt");

                // Opción pro: Actualizar el saludo si el usuario deja el programa abierto y cambia de mañana a tarde
                if (DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
                {
                    ActualizarSaludo();
                }
            };
            _timerReloj.Start();
        }

        private void ActualizarSaludo()
        {
            int hora = DateTime.Now.Hour;
            string usuario = "Dr. Mario"; // En el futuro puedes reemplazar esto leyendo la variable de sesión

            if (hora >= 5 && hora < 12)
                SaludoDinamico = $"¡Buenos días, {usuario}!";
            else if (hora >= 12 && hora < 19)
                SaludoDinamico = $"¡Buenas tardes, {usuario}!";
            else
                SaludoDinamico = $"¡Buenas noches, {usuario}!";
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