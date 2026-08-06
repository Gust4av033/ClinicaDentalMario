using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pagos
{
    public class EstadoCuentaViewModel : ViewModelBase
    {
        private decimal _totalCargos;
        public decimal TotalCargos
        {
            get => _totalCargos;
            set => SetProperty(ref _totalCargos, value);
        }

        private decimal _totalPagado;
        public decimal TotalPagado
        {
            get => _totalPagado;
            set => SetProperty(ref _totalPagado, value);
        }

        public decimal SaldoPendiente => TotalCargos - TotalPagado;

        public ICommand CargarEstadoCommand { get; }

        public EstadoCuentaViewModel(int idPaciente)
        {
            Titulo = "Estado de Cuenta";
            CargarEstadoCommand = new RelayCommand(CargarEstado);
        }

        private void CargarEstado(object? parameter)
        {
            // Consumir Vista SQL: vwSaldoPacientes
            // Notificar cambio en propiedad calculada
            OnPropertyChanged(nameof(SaldoPendiente));
        }
    }
}
