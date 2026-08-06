using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class NuevoPacienteViewModel : ViewModelBase
    {
       // [cite_start]// Enlazamos directamente a un nuevo objeto Paciente [cite: 453-460]
        private PacienteModel _nuevoPaciente;
        public PacienteModel NuevoPaciente
        {
            get => _nuevoPaciente;
            set => SetProperty(ref _nuevoPaciente, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoPacienteViewModel()
        {
            Titulo = "Registrar Nuevo Paciente";

            // Inicializamos con valores por defecto
            _nuevoPaciente = new PacienteModel
            {
                FechaRegistro = DateTime.Now,
                FechaNacimiento = DateTime.Now.AddYears(-20), // Valor base sugerido
                Activo = true
            };

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            // Validación básica manual
            if (string.IsNullOrWhiteSpace(NuevoPaciente.NombreCompleto))
            {
                MensajeError = "El nombre completo es obligatorio.";
                return;
            }

            MensajeError = string.Empty;
            EstaCargando = true;

            // Aquí llamaremos al PacienteRepository para ejecutar sp_InsertarPaciente [cite: 530, 531]

            EstaCargando = false;
        }

        private void Cancelar(object? parameter)
        {
            // Lógica para regresar a ListaPacientes sin guardar
        }
    }
}
