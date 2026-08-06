using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Base
{
    public class MainViewModel : ViewModelBase
    {
        // Esta propiedad es la que WPF leerá para saber qué pantalla mostrar
        private ViewModelBase _vistaActual;
        public ViewModelBase VistaActual
        {
            get => _vistaActual;
            set => SetProperty(ref _vistaActual, value);
        }

        // Comandos de navegación para tu menú lateral
        public ICommand NavegarDashboardCommand { get; }
        public ICommand NavegarPacientesCommand { get; }
        public ICommand NavegarAgendaCommand { get; }

        public MainViewModel()
        {
            Titulo = "Clínica Dental Mario - Panel Principal";

            // Inicializamos los comandos de navegación
            NavegarDashboardCommand = new RelayCommand((_) => VistaActual = new Dashboard.DashboardViewModel());
            NavegarPacientesCommand = new RelayCommand((_) => VistaActual = new Pacientes.ListaPacientesViewModel());
            NavegarAgendaCommand = new RelayCommand((_) => VistaActual = new Agenda.AgendaViewModel());

            // Arrancamos la app mostrando el Dashboard por defecto
            VistaActual = new Dashboard.DashboardViewModel();
        }
    }
}
