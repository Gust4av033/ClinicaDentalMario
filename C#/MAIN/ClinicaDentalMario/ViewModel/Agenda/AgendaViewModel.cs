using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Agenda
{
    public class AgendaViewModel : ViewModelBase
    {
        private ObservableCollection<CitaModel> _citas = new ObservableCollection<CitaModel>();
        public ObservableCollection<CitaModel> Citas
        {
            get => _citas;
            set => SetProperty(ref _citas, value);
        }

        private DateTime _fechaSeleccionada = DateTime.Today;
        public DateTime FechaSeleccionada
        {
            get => _fechaSeleccionada;
            set => SetProperty(ref _fechaSeleccionada, value);
        }

        private CitaModel? _citaSeleccionada;
        public CitaModel? CitaSeleccionada
        {
            get => _citaSeleccionada;
            set => SetProperty(ref _citaSeleccionada, value);
        }

        public ICommand BuscarCitasCommand { get; }
        public ICommand NuevaCitaCommand { get; }
        public ICommand EditarCitaCommand { get; }

        public AgendaViewModel()
        {
            Titulo = "Agenda Diaria";

            BuscarCitasCommand = new RelayCommand(BuscarCitas);
            NuevaCitaCommand = new RelayCommand(NuevaCita);
            EditarCitaCommand = new RelayCommand(EditarCita, (param) => CitaSeleccionada != null);
        }

        private void BuscarCitas(object? parameter)
        {
            // Lógica para cargar las citas de la FechaSeleccionada usando CitaRepository
        }

        private void NuevaCita(object? parameter) { /* Navegar a NuevaCitaViewModel */ }
        private void EditarCita(object? parameter) { /* Navegar a EditarCitaViewModel */ }
    }
}
