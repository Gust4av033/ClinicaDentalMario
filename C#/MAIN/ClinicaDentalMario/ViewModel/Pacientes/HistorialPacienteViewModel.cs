using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class HistorialPacienteViewModel : ViewModelBase
    {
        // El paciente al que le pertenece este historial
        private PacienteModel _paciente;
        public PacienteModel Paciente
        {
            get => _paciente;
            set => SetProperty(ref _paciente, value);
        }

        // Lista observable que actualizará el DataGrid automáticamente al agregar consultas
        private ObservableCollection<HistorialPacienteViewModel> _historialConsultas = new ObservableCollection<HistorialPacienteViewModel>();
        public ObservableCollection<HistorialPacienteViewModel> HistorialConsultas
        {
            get => _historialConsultas;
            set => SetProperty(ref _historialConsultas, value);
        }

        private HistorialPacienteViewModel? _consultaSeleccionada;
        public HistorialPacienteViewModel? ConsultaSeleccionada
        {
            get => _consultaSeleccionada;
            set => SetProperty(ref _consultaSeleccionada, value);
        }

        public ICommand NuevaConsultaCommand { get; }
        public ICommand VolverCommand { get; }

        public HistorialPacienteViewModel(PacienteModel paciente)
        {
            Titulo = $"Historial Clínico - {paciente.NombreCompleto}";
            _paciente = paciente;

            NuevaConsultaCommand = new RelayCommand(NuevaConsulta);
            VolverCommand = new RelayCommand(Volver);

            // A futuro: CargarDashboardCommand.Execute(null) o método similar 
           // [cite_start]// para ejecutar sp_ListarConsultasPaciente [cite: 512, 513] y llenar _historialConsultas.
        }

        private void NuevaConsulta(object? parameter)
        {
            // Lógica para abrir una ventana/modal y registrar una nueva entrada clínica
        }

        private void Volver(object? parameter)
        {
            // Navegar de regreso
        }
    }
}
