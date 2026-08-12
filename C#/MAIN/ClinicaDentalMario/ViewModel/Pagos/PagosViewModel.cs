using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pagos
{
    public class PagosViewModel : ViewModelBase
    {
        private ObservableCollection<PagoModel> _historialPagos = new();
        public ObservableCollection<PagoModel> HistorialPagos
        {
            get => _historialPagos;
            set => SetProperty(ref _historialPagos, value);
        }

        private PagoModel? _pagoSeleccionado;
        public PagoModel? PagoSeleccionado
        {
            get => _pagoSeleccionado;
            set => SetProperty(ref _pagoSeleccionado, value);
        }

        public ICommand NuevoPagoCommand { get; }
        public ICommand VerDetalleCommand { get; }

        public PagosViewModel()
        {
            Titulo = "Historial de Pagos";

            NuevoPagoCommand = new RelayCommand(NuevoPago);
            VerDetalleCommand = new RelayCommand(VerDetalle, (param) => PagoSeleccionado != null);
        }

        private void NuevoPago(object? parameter) { /* Navegar a NuevoPagoViewModel */ }
        private void VerDetalle(object? parameter) { /* Lógica para ver detalles o imprimir recibo */ }
    }
}
