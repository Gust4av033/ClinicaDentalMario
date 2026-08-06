using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Personal
{
    public class EditarDoctorViewModel : ViewModelBase
    {
        private DoctorModel _doctorEditado;
        public DoctorModel DoctorEditado
        {
            get => _doctorEditado;
            set => SetProperty(ref _doctorEditado, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public EditarDoctorViewModel(DoctorModel doctor)
        {
            Titulo = "Editar Información del Doctor";
            _doctorEditado = doctor;

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            // Ejecutar sp para editar doctor
        }

        private void Cancelar(object? parameter) { /* Volver atrás */ }
    }
}
