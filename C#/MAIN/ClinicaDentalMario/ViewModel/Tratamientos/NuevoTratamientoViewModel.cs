using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class NuevoTratamientoViewModel : ViewModelBase
    {
        private TratamientoPacienteModel _nuevoTratamiento;
        public TratamientoPacienteModel NuevoTratamiento
        {
            get => _nuevoTratamiento;
            set => SetProperty(ref _nuevoTratamiento, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoTratamientoViewModel(int idPaciente)
        {
            Titulo = "Asignar Tratamiento";
            _nuevoTratamiento = new TratamientoPacienteModel
            {
                IdPaciente = idPaciente,
                FechaInicio = DateTime.Now,
                Estado = "Pendiente"
            };

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
           // [cite_start]// sp_CrearTratamiento [cite: 582, 583]
        }

        private void Cancelar(object? parameter) { /* Volver atrás */ }
    }
}
