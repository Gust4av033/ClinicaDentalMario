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
    public class NuevoDoctorViewModel : ViewModelBase
    {
        private DoctorModel _nuevoDoctor;
        public DoctorModel  NuevoDoctor
        {
            get => _nuevoDoctor;
            set => SetProperty(ref _nuevoDoctor, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoDoctorViewModel()
        {
            Titulo = "Registrar Nuevo Doctor";
            _nuevoDoctor = new DoctorModel { Activo = true };

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            // Validar NombreCompleto y llamar al repositorio
        }

        private void Cancelar(object? parameter) { /* Volver atrás */ }
    }
}
