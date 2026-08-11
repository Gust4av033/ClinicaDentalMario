using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Agenda; // Para regresar a la Agenda
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class NuevaCitaViewModel : ViewModelBase
    {
        private readonly Action<object> _cambiarVista;
        private readonly PacienteRepository _pacienteRepository;
        private readonly DoctorRepository _doctorRepository;
        private readonly CitaRepository _citaRepository;

        // --- LISTAS PARA LOS COMBOBOXES ---
        private ObservableCollection<PacienteModel> _listaPacientes = new();
        public ObservableCollection<PacienteModel> ListaPacientes
        {
            get => _listaPacientes;
            set => SetProperty(ref _listaPacientes, value);
        }

        private ObservableCollection<DoctorModel> _listaDoctores = new();
        public ObservableCollection<DoctorModel> ListaDoctores
        {
            get => _listaDoctores;
            set => SetProperty(ref _listaDoctores, value);
        }

        // --- SELECCIONES DEL USUARIO ---
        private PacienteModel _pacienteSeleccionado;
        public PacienteModel PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set => SetProperty(ref _pacienteSeleccionado, value);
        }

        private DoctorModel _doctorSeleccionado;
        public DoctorModel DoctorSeleccionado
        {
            get => _doctorSeleccionado;
            set => SetProperty(ref _doctorSeleccionado, value);
        }

        private DateTime _fechaSeleccionada;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set => SetProperty(ref _fechaSeleccionada, value);
        }

        // Usamos string para la cajita de texto (Ej. "14:30")
        private string _horaSeleccionada;
        public string HoraSeleccionada
        {
            get => _horaSeleccionada;
            set => SetProperty(ref _horaSeleccionada, value);
        }

        private CitaModel _nuevaCita;
        public CitaModel NuevaCita
        {
            get => _nuevaCita;
            set => SetProperty(ref _nuevaCita, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevaCitaViewModel(Action<object> cambiarVista)
        {
            Titulo = "Agendar Nueva Cita";
            _cambiarVista = cambiarVista;

            _pacienteRepository = new PacienteRepository();

            // 🔥 QUITA LAS DOS DIAGONALES DE AQUÍ:
            _doctorRepository = new DoctorRepository();
            _citaRepository = new CitaRepository();

            _nuevaCita = new CitaModel();
            FechaSeleccionada = DateTime.Today.AddDays(1); // Mañana por defecto
            HoraSeleccionada = "10:00"; // Hora sugerida

            GuardarCommand = new RelayCommand(async (param) => await GuardarAsync());
            CancelarCommand = new RelayCommand(VolverAAgenda);

            _ = CargarListasAsync();
        }

        private async Task CargarListasAsync()
        {
            try
            {
                var pacientes = await _pacienteRepository.ObtenerTodosAsync();
                ListaPacientes = new ObservableCollection<PacienteModel>(pacientes);

                var doctores = await _doctorRepository.ObtenerDoctoresActivosAsync();
                
                ListaDoctores = new ObservableCollection<DoctorModel>(doctores);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al cargar listas: " + ex.Message;
            }
        }

        private async Task GuardarAsync()
        {
            if (PacienteSeleccionado == null || DoctorSeleccionado == null)
            {
                MensajeError = "Debe seleccionar un Paciente y un Doctor de la lista.";
                return;
            }

            if (!TimeSpan.TryParse(HoraSeleccionada, out TimeSpan hora))
            {
                MensajeError = "Formato de hora inválido. Use HH:mm (ej. 14:30 o 09:00).";
                return;
            }

            DateTime fechaHoraCitaFinal = FechaSeleccionada.Date.Add(hora);

            EstaCargando = true;
            try
            {
                NuevaCita.IdPaciente = PacienteSeleccionado.IdPaciente;
                NuevaCita.IdDoctor = DoctorSeleccionado.IdDoctor;
                NuevaCita.FechaHora = fechaHoraCitaFinal;
                NuevaCita.IdEstado = 1; // 1 = Pendiente (Según tu catálogo de BD)

                // 🔥 QUITA LAS DOS DIAGONALES DE AQUÍ PARA QUE GUARDE EN LA BASE:
                await _citaRepository.InsertarAsync(NuevaCita);

                MessageBox.Show($"Cita agendada para el {fechaHoraCitaFinal:dd/MM/yyyy} a las {fechaHoraCitaFinal:HH:mm}.", "Cita Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
                VolverAAgenda(null);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al agendar: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void VolverAAgenda(object? parameter)
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