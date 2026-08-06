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
    public class EditarCitaViewModel : ViewModelBase
    {
        private CitaModel _citaEditada;
        public CitaModel CitaEditada
        {
            get => _citaEditada;
            set => SetProperty(ref _citaEditada, value);
        }

        public ICommand ActualizarCommand { get; }
        public ICommand CancelarCitaCommand { get; }
        public ICommand VolverCommand { get; }

        public EditarCitaViewModel(CitaModel cita)
        {
            Titulo = "Modificar Cita";
            _citaEditada = cita;

            ActualizarCommand = new RelayCommand(Actualizar);
            CancelarCitaCommand = new RelayCommand(CancelarCita);
            VolverCommand = new RelayCommand(Volver);
        }

        private void Actualizar(object? parameter)
        {
            //[cite_start]// Llamar a sp_EditarCita [cite: 577]
        }

        private void CancelarCita(object? parameter)
        {
            //[cite_start]// Llamar a sp_CancelarCita [cite: 578]
        }

        private void Volver(object? parameter) { /* Navegar atrás */ }
    }
}
