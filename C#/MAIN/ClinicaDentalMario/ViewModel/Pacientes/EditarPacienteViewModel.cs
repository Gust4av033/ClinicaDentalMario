using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class EditarPacienteViewModel : ViewModelBase 
    {
       // [cite_start]// Mapea directamente a tu tabla Pacientes [cite: 546-547, 600-607]
        private PacienteModel _pacienteEditado;
        public PacienteModel PacienteEditado
        {
            get => _pacienteEditado;
            set => SetProperty(ref _pacienteEditado, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public EditarPacienteViewModel(PacienteModel paciente)
        {
            Titulo = "Editar Información del Paciente";

            // Recibimos el paciente seleccionado desde la lista
            _pacienteEditado = paciente;

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            // Validaciones manuales básicas
            if (string.IsNullOrWhiteSpace(PacienteEditado.NombreCompleto))
            {
                MensajeError = "El nombre completo no puede quedar vacío.";
                return;
            }

            MensajeError = string.Empty;
            EstaCargando = true;

           // [cite_start]// Aquí llamaremos al PacienteRepository para ejecutar sp_EditarPaciente [cite: 505-507]

            EstaCargando = false;
        }

        private void Cancelar(object? parameter)
        {
            // Lógica para regresar al listado descartando los cambios en UI
        }
    }
}
