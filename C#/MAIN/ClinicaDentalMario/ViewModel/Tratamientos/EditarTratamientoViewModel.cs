using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class EditarTratamientoViewModel : ViewModelBase
    {
        private TratamientoPacienteModel _tratamientoEditado;
        public TratamientoPacienteModel TratamientoEditado
        {
            get => _tratamientoEditado;
            set => SetProperty(ref _tratamientoEditado, value);
        }

        public ICommand FinalizarCommand { get; }
        public ICommand GuardarCommand { get; }

        public EditarTratamientoViewModel(TratamientoPacienteModel tratamiento)
        {
            Titulo = "Editar Tratamiento";
            _tratamientoEditado = tratamiento;

            GuardarCommand = new RelayCommand(Guardar);
            FinalizarCommand = new RelayCommand(Finalizar);
        }

        private void Guardar(object? parameter)
        {
            //[cite_start]// sp_ActualizarEstadoTratamiento [cite: 587]
        }

        private void Finalizar(object? parameter)
        {
            TratamientoEditado.Estado = "Finalizado";
            //[cite_start]// sp_FinalizarTratamiento 
        }
    }
}
