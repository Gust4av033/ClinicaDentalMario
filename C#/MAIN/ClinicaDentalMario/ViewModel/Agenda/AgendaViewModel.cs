using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class AgendaViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly CitaRepository _citaRepository;

        private ObservableCollection<dynamic> _citasDelDia = new();
        public ObservableCollection<dynamic> CitasDelDia
        {
            get => _citasDelDia;
            set => SetProperty(ref _citasDelDia, value);
        }

        private DateTime _fechaSeleccionada = DateTime.Today;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set
            {
                if (SetProperty(ref _fechaSeleccionada, value))
                {
                    _ = CargarCitasDelDiaAsync();
                    CerrarDetalle(null); // Ocultar el panel si cambian de día
                }
            }
        }

        private dynamic? _citaSeleccionada;
        public dynamic? CitaSeleccionada
        {
            get => _citaSeleccionada;
            set
            {
                if (SetProperty(ref _citaSeleccionada, value))
                {
                    // Lógica del Panel: Si seleccionan algo, se muestra. Si no, se oculta.
                    PanelDetalleVisibility = value != null ? Visibility.Visible : Visibility.Collapsed;
                    AnchoPanelDetalle = value != null ? 300 : 0;
                }
            }
        }

        // --- PROPIEDADES VISUALES PARA EL PANEL LATERAL ---
        private Visibility _panelDetalleVisibility = Visibility.Collapsed;
        public Visibility PanelDetalleVisibility
        {
            get => _panelDetalleVisibility;
            set => SetProperty(ref _panelDetalleVisibility, value);
        }

        private double _anchoPanelDetalle = 0;
        public double AnchoPanelDetalle
        {
            get => _anchoPanelDetalle;
            set => SetProperty(ref _anchoPanelDetalle, value);
        }

        // --- COMANDOS ---
        public ICommand NuevaCitaCommand { get; }
        public ICommand EditarCitaCommand { get; }
        public ICommand CancelarCitaCommand { get; }
        public ICommand FinalizarCitaCommand { get; } // 🔥 NUEVO COMANDO 🔥
        public ICommand CerrarDetalleCommand { get; }

        public AgendaViewModel(Action<object> cambiarVista)
        {
            Titulo = "Agenda Diaria";
            _cambiarVista = cambiarVista;
            _citaRepository = new CitaRepository();

            NuevaCitaCommand = new RelayCommand(AbrirNuevaCita);
            EditarCitaCommand = new RelayCommand(AbrirEditarCita);
            CancelarCitaCommand = new RelayCommand(async (param) => await CancelarCitaAsync(param));
            FinalizarCitaCommand = new RelayCommand(async (param) => await FinalizarCitaAsync(param)); // 🔥 INICIALIZADO 🔥
            CerrarDetalleCommand = new RelayCommand(CerrarDetalle);

            _ = CargarCitasDelDiaAsync();
        }

        private async Task CargarCitasDelDiaAsync()
        {
            EstaCargando = true;
            try
            {
                var citas = await _citaRepository.ObtenerCitasPorFechaAsync(FechaSeleccionada);
                CitasDelDia = new ObservableCollection<dynamic>(citas);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la agenda: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EstaCargando = false; }
        }

        private void CerrarDetalle(object? parameter)
        {
            CitaSeleccionada = null; // Esto automáticamente oculta el panel
        }

        private void AbrirNuevaCita(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaNuevaCita = new NuevaCitaView();
                vistaNuevaCita.DataContext = new NuevaCitaViewModel(_cambiarVista);
                _cambiarVista(vistaNuevaCita);
            }
        }

        private void AbrirEditarCita(object? parameter)
        {
            if (CitaSeleccionada != null && _cambiarVista != null)
            {
                // 🔥 CAMBIO: "Atendida" en vez de "Finalizada"
                if (CitaSeleccionada.Estado == "Atendida" || CitaSeleccionada.Estado == "Cancelada")
                {
                    MessageBox.Show("No puedes reprogramar una cita que ya fue atendida o cancelada.", "Acción denegada", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var vistaEditar = new EditarCitaView();
                var viewModelEditar = new EditarCitaViewModel(CitaSeleccionada, _cambiarVista);
                vistaEditar.DataContext = viewModelEditar;
                _cambiarVista(vistaEditar);
            }
        }

        private async Task CancelarCitaAsync(object? parameter)
        {
            if (CitaSeleccionada != null)
            {
                // 🔥 CAMBIO: "Atendida" en vez de "Finalizada"
                if (CitaSeleccionada.Estado == "Atendida" || CitaSeleccionada.Estado == "Cancelada")
                {
                    MessageBox.Show("Esta cita ya está atendida o cancelada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"¿Deseas cancelar la cita de {CitaSeleccionada.Paciente}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _citaRepository.CancelarCitaAsync((int)CitaSeleccionada.IdCita);
                    MessageBox.Show("Cita cancelada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    CerrarDetalle(null);
                    await CargarCitasDelDiaAsync();
                }
            }
        }


        // 🔥 NUEVA FUNCIÓN PARA FINALIZAR CITAS 🔥
        private async Task FinalizarCitaAsync(object? parameter)
        {
            if (CitaSeleccionada != null)
            {
                // 🔥 CAMBIO: "Atendida" en vez de "Finalizada"
                if (CitaSeleccionada.Estado == "Atendida" || CitaSeleccionada.Estado == "Cancelada")
                {
                    MessageBox.Show("Esta cita ya está cerrada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"¿El paciente {CitaSeleccionada.Paciente} ya fue atendido? Se marcará la cita como Atendida.", "Finalizar Cita", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 🔥 CAMBIO VITAL: Le mandamos la palabra "Atendida" a SQL
                    await _citaRepository.CambiarEstadoCitaAsync((int)CitaSeleccionada.IdCita, "Atendida");
                    MessageBox.Show("Cita marcada como Atendida con éxito.", "Excelente", MessageBoxButton.OK, MessageBoxImage.Information);

                    CerrarDetalle(null);
                    await CargarCitasDelDiaAsync();
                }
            }
        }
    }
}