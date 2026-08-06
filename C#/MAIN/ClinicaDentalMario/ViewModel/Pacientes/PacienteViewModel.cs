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
    public class PacienteViewModel : ViewModelBase  
    {
        //[cite_start]// El paciente completo con todos sus datos [cite: 600-607]
        private PacienteModel _pacienteActual;
        public PacienteModel PacienteActual
        {
            get => _pacienteActual;
            set => SetProperty(ref _pacienteActual, value);
        }

        // Comando para regresar al listado
        public ICommand VolverCommand { get; }

        public PacienteViewModel(PacienteModel paciente)
        {
            Titulo = "Detalle del Paciente";
            _pacienteActual = paciente;

            VolverCommand = new RelayCommand(Volver);
        }

        private void Volver(object? parameter)
        {
            // Lógica de navegación para regresar a ListaPacientesViewModel
        }
    }
}
