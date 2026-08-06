using ClinicaDentalMario.Models;    
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Odontograma
{
    public class OdontogramaViewModel : ViewModelBase
    {
        private ObservableCollection<OdontogramaModel> _registrosDentales = new();
        public ObservableCollection<OdontogramaModel> RegistrosDentales
        {
            get => _registrosDentales;
            set => SetProperty(ref _registrosDentales, value);
        }

        private PiezaDentalViewModel? _piezaSeleccionada;
        public PiezaDentalViewModel? PiezaSeleccionada
        {
            get => _piezaSeleccionada;
            set => SetProperty(ref _piezaSeleccionada, value);
        }

        public ICommand ActualizarPiezaCommand { get; }

        public OdontogramaViewModel(int idPaciente)
        {
            Titulo = "Odontograma del Paciente";
            ActualizarPiezaCommand = new RelayCommand(ActualizarPieza, (param) => PiezaSeleccionada != null);
        }

        private void ActualizarPieza(object? parameter)
        {
            // Lógica para ejecutar sp_ActualizarPiezaDental
        }
    }
}
