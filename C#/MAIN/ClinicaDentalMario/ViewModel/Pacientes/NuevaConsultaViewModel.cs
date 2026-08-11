using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views.Pacientes;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class NuevaConsultaViewModel : ViewModelBase
    {
        private readonly HistorialClinicoRepository _historialRepo;
        private readonly Action<object> _cambiarVista;

        private PacienteModel _pacienteActual;
        public PacienteModel PacienteActual
        {
            get => _pacienteActual;
            set => SetProperty(ref _pacienteActual, value);
        }

        private HistorialClinicoModel _nuevaConsulta;
        public HistorialClinicoModel NuevaConsulta
        {
            get => _nuevaConsulta;
            set => SetProperty(ref _nuevaConsulta, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevaConsultaViewModel(PacienteModel paciente, Action<object> cambiarVista)
        {
            Titulo = "Registrar Nueva Consulta Médica";
            PacienteActual = paciente;
            _cambiarVista = cambiarVista;
            _historialRepo = new HistorialClinicoRepository();

            NuevaConsulta = new HistorialClinicoModel
            {
                IdPaciente = paciente.IdPaciente,
                IdDoctor = 1, // Por ahora quemamos el ID del doctor activo
                FechaConsulta = DateTime.Now
            };

            GuardarCommand = new RelayCommand(async (param) => await GuardarAsync());
            CancelarCommand = new RelayCommand(VolverAHistorial);
        }

        private async Task GuardarAsync()
        {
            if (string.IsNullOrWhiteSpace(NuevaConsulta.MotivoConsulta) || string.IsNullOrWhiteSpace(NuevaConsulta.Diagnostico))
            {
                MensajeError = "Debe llenar al menos el Motivo y el Diagnóstico.";
                return;
            }

            EstaCargando = true;
            MensajeError = string.Empty;

            try
            {
                // Usamos el repositorio que ya tenemos para insertar en el historial
                await _historialRepo.InsertarConsultaAsync(NuevaConsulta);

                MessageBox.Show("Nueva consulta agregada al expediente con éxito.", "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
                VolverAHistorial(null);
            }
            catch (Exception ex)
            {
                MensajeError = "Error al guardar consulta: " + ex.Message;
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void VolverAHistorial(object? parameter)
        {
            if (_cambiarVista != null)
            {
                var vistaHistorial = new HistorialPacienteView();
                vistaHistorial.DataContext = new HistorialPacienteViewModel(PacienteActual, _cambiarVista);
                _cambiarVista(vistaHistorial);
            }
        }
    }
}