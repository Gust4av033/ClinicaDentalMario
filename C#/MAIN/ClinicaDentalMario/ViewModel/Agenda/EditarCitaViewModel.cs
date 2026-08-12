using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class EditarCitaViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly CitaRepository _citaRepo;

        private int _idCitaActual;

        // Variables para la interfaz gráfica
        private string _nombrePaciente;
        public string NombrePaciente
        {
            get => _nombrePaciente;
            set => SetProperty(ref _nombrePaciente, value);
        }

        private DateTime _fechaSeleccionada;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set => SetProperty(ref _fechaSeleccionada, value);
        }

        private string _horaSeleccionada;
        public string HoraSeleccionada
        {
            get => _horaSeleccionada;
            set => SetProperty(ref _horaSeleccionada, value);
        }

        private string _observaciones;
        public string Observaciones
        {
            get => _observaciones;
            set => SetProperty(ref _observaciones, value);
        }

        public ICommand ActualizarCommand { get; }
        public ICommand VolverCommand { get; }

        public EditarCitaViewModel(dynamic cita, Action<object> cambiarVista)
        {
            Titulo = "Modificar o Reprogramar Cita";
            _cambiarVista = cambiarVista;
            _citaRepo = new CitaRepository();

            // Desempaquetamos los datos que nos mandó la tabla
            _idCitaActual = cita.IdCita;
            NombrePaciente = cita.Paciente;
            DateTime fechaOriginal = cita.FechaHora;

            FechaSeleccionada = fechaOriginal.Date;
            HoraSeleccionada = fechaOriginal.ToString("HH:mm");
            Observaciones = cita.Observaciones;

            ActualizarCommand = new RelayCommand(async (param) => await ActualizarAsync());
            VolverCommand = new RelayCommand(Volver);
        }

        private async Task ActualizarAsync()
        {
            if (!TimeSpan.TryParse(HoraSeleccionada, out TimeSpan hora))
            {
                MessageBox.Show("Formato de hora inválido. Use HH:mm (ej. 14:30).", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime nuevaFechaHora = FechaSeleccionada.Date.Add(hora);

            EstaCargando = true;
            try
            {
                await _citaRepo.ActualizarCitaAsync(_idCitaActual, nuevaFechaHora, Observaciones);

                MessageBox.Show("Cita reprogramada con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                Volver(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar la cita: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void Volver(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaAgenda = new AgendaView();
                vistaAgenda.DataContext = new AgendaViewModel(_cambiarVista);
                _cambiarVista(vistaAgenda);
            }
        }
    }
}