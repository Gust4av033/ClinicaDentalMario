using ClinicaDentalMario.Models;
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

        // Usamos dynamic porque el JOIN trae columnas combinadas (Nombre Paciente, Doctor, etc.)
        private ObservableCollection<dynamic> _citasDelDia = new ObservableCollection<dynamic>();
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
                    // MAGIA: Al cambiar el día en el DatePicker, actualiza la tabla de inmediato
                    _ = CargarCitasDelDiaAsync();
                }
            }
        }

        private dynamic? _citaSeleccionada;
        public dynamic? CitaSeleccionada
        {
            get => _citaSeleccionada;
            set => SetProperty(ref _citaSeleccionada, value);
        }

        public ICommand NuevaCitaCommand { get; }
        public ICommand EditarCitaCommand { get; }
        public ICommand CancelarCitaCommand { get; }

        public AgendaViewModel(Action<object> cambiarVista)
        {
            Titulo = "Agenda Diaria";
            _cambiarVista = cambiarVista;
            _citaRepository = new CitaRepository();

            NuevaCitaCommand = new RelayCommand(AbrirNuevaCita);
            EditarCitaCommand = new RelayCommand(AbrirEditarCita);
            CancelarCitaCommand = new RelayCommand(async (param) => await CancelarCitaAsync(param));

            // Cargar citas del día de hoy al abrir la pantalla
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
            finally
            {
                EstaCargando = false;
            }
        }

        private void AbrirNuevaCita(object? parameter)
        {
            if (_cambiarVista != null)
            {
                // Navegamos a la vista de Nueva Cita
                var vistaNuevaCita = new NuevaCitaView();
                vistaNuevaCita.DataContext = new NuevaCitaViewModel(_cambiarVista);
                _cambiarVista(vistaNuevaCita);
            }
        }

        private void AbrirEditarCita(object? parameter)
        {
            var citaAEditar = parameter as dynamic ?? CitaSeleccionada;
            if (citaAEditar != null && _cambiarVista != null)
            {
                // Aquí deberías crear tu EditarCitaView visualmente, pero ya le pasamos los datos
                var vistaEditar = new EditarCitaView(); // Asumiendo que la crearás
                var viewModelEditar = new EditarCitaViewModel(citaAEditar, _cambiarVista);
                vistaEditar.DataContext = viewModelEditar;
                _cambiarVista(vistaEditar);
            }
        }

        private async Task CancelarCitaAsync(object? parameter)
        {
            var citaACancelar = parameter as dynamic ?? CitaSeleccionada;
            if (citaACancelar != null)
            {
                var result = MessageBox.Show($"¿Deseas cancelar la cita de {citaACancelar.Paciente}?", "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _citaRepository.CancelarCitaAsync(citaACancelar.IdCita);
                    MessageBox.Show("Cita cancelada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    await CargarCitasDelDiaAsync(); // Recargamos la tabla
                }
            }
        }
    }
}