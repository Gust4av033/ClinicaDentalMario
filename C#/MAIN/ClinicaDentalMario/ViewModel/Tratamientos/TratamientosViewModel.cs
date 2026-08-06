using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class TratamientosViewModel : ViewModelBase
    {
        private TratamientoPacienteModel _pacienteContexto;
        public TratamientoPacienteModel PacienteContexto
        {
            get => _pacienteContexto;
            set => SetProperty(ref _pacienteContexto, value);
        }

        private ObservableCollection<TratamientosViewModel> _tratamientos = new();
        public ObservableCollection<TratamientosViewModel> Tratamientos
        {
            get => _tratamientos;
            set => SetProperty(ref _tratamientos, value);
        }

        private TratamientosViewModel? _tratamientoSeleccionado;
        public TratamientosViewModel? TratamientoSeleccionado
        {
            get => _tratamientoSeleccionado;
            set => SetProperty(ref _tratamientoSeleccionado, value);
        }

        public ICommand NuevoTratamientoCommand { get; }
        public ICommand EditarTratamientoCommand { get; }

        public TratamientosViewModel(TratamientoPacienteModel paciente)
        {
            Titulo = $"Tratamientos de {paciente.NombreCompleto}";
            _pacienteContexto = paciente;

            NuevoTratamientoCommand = new RelayCommand(Nuevo);
            EditarTratamientoCommand = new RelayCommand(Editar, (param) => TratamientoSeleccionado != null);
        }

        private void Nuevo(object? parameter) { /* Ir a NuevoTratamientoViewModel */ }
        private void Editar(object? parameter) { /* Ir a EditarTratamientoViewModel */ }
    }
}
