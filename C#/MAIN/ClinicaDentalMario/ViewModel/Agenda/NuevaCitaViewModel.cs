using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class NuevaCitaViewModel : ViewModelBase
    {
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

        public NuevaCitaViewModel()
        {
            Titulo = "Agendar Nueva Cita";
            _nuevaCita = new CitaModel { FechaHora = DateTime.Now.AddDays(1) }; // Por defecto mañana

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            if (NuevaCita.IdPaciente == 0 || NuevaCita.IdDoctor == 0)
            {
                MensajeError = "Debe seleccionar un Paciente y un Doctor.";
                return;
            }

            EstaCargando = true;
            //[cite_start]// Llamar a sp_AgendarCita [cite: 576, 577]
            EstaCargando = false;
        }

        private void Cancelar(object? parameter) { /* Volver a AgendaViewModel */ }
    }
}
