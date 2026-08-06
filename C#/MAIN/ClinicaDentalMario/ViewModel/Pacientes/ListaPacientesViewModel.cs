using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;

namespace ClinicaDentalMario.ViewModel.Pacientes
{
    public class ListaPacientesViewModel : ViewModelBase
    {
        // Colección que notifica a la tabla de WPF cuando hay cambios
        private ObservableCollection<PacienteModel> _pacientes = new ObservableCollection<PacienteModel>();
        public ObservableCollection<PacienteModel> Pacientes
        {
            get => _pacientes;
            set => SetProperty(ref _pacientes, value);
        }

        private string _textoBusqueda = string.Empty;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set => SetProperty(ref _textoBusqueda, value);
        }

        private PacienteModel? _pacienteSeleccionado;
        public PacienteModel? PacienteSeleccionado
        {
            get => _pacienteSeleccionado;
            set => SetProperty(ref _pacienteSeleccionado, value);
        }

        // Comandos para los botones
        public ICommand BuscarCommand { get; }
        public ICommand NuevoPacienteCommand { get; }
        public ICommand EditarPacienteCommand { get; }

        public ListaPacientesViewModel()
        {
            Titulo = "Listado de Pacientes";

            // Inicialización de comandos
            BuscarCommand = new RelayCommand(Buscar);
            NuevoPacienteCommand = new RelayCommand(NuevoPaciente);

            // Editar solo se habilita si hay un paciente seleccionado
            EditarPacienteCommand = new RelayCommand(EditarPaciente, (param) => PacienteSeleccionado != null);
        }

        private void Buscar(object? parameter)
        {
            // Lógica para filtrar por Nombre o DUI en la BD [cite: 536]
        }

        private void NuevoPaciente(object? parameter)
        {
            // Lógica para navegar a NuevoPacienteViewModel
        }

        private void EditarPaciente(object? parameter)
        {
            if (PacienteSeleccionado != null)
            {
                // Lógica para navegar a EditarPacienteViewModel pasando el ID
            }
        }
    }
}
