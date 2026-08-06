using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Personal
{
    public class DoctoresViewModel : ViewModelBase
    {
        private ObservableCollection<DoctorModel> _doctores = new();
        public ObservableCollection<DoctorModel> Doctores
        {
            get => _doctores;
            set => SetProperty(ref _doctores, value);
        }

        private DoctorModel? _doctorSeleccionado;
        public DoctorModel? DoctorSeleccionado
        {
            get => _doctorSeleccionado;
            set => SetProperty(ref _doctorSeleccionado, value);
        }

        public ICommand NuevoDoctorCommand { get; }
        public ICommand EditarDoctorCommand { get; }

        public DoctoresViewModel()
        {
            Titulo = "Gestión de Personal Médico";

            NuevoDoctorCommand = new RelayCommand(NuevoDoctor);
            EditarDoctorCommand = new RelayCommand(EditarDoctor, (param) => DoctorSeleccionado != null);
        }

        private void NuevoDoctor(object? parameter) { /* Navegar a NuevoDoctorViewModel */ }
        private void EditarDoctor(object? parameter) { /* Navegar a EditarDoctorViewModel */ }
    }
}
