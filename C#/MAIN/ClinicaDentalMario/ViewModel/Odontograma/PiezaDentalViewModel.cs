using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Models;

namespace ClinicaDentalMario.ViewModel.Odontograma
{
    public class PiezaDentalViewModel : ViewModelBase
    {
        private int _numeroPieza;
        public int NumeroPieza
        {
            get => _numeroPieza;
            set => SetProperty(ref _numeroPieza, value);
        }

        private string _estadoActual = "Sano";
        public string EstadoActual
        {
            get => _estadoActual;
            set => SetProperty(ref _estadoActual, value);
        }

        private string _colorHex = "#FFFFFF";
        public string ColorHex
        {
            get => _colorHex;
            set => SetProperty(ref _colorHex, value);
        }
    }
}
