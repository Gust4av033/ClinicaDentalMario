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
    public class NuevoPagoViewModel : ViewModelBase
    {
        private PagoModel _nuevoPago;
        public PagoModel NuevoPago
        {
            get => _nuevoPago;
            set => SetProperty(ref _nuevoPago, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoPagoViewModel(int idTratamiento)
        {
            Titulo = "Registrar Abono";

            _nuevoPago = new PagoModel
            {
                IdTratamientoPaciente = idTratamiento,
                FechaPago = DateTime.Now,
                Monto = 0 // DECIMAL(10,2) [cite: 652]
            };

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            if (NuevoPago.Monto <= 0)
            {
                MensajeError = "El monto debe ser mayor a cero.";
                return;
            }

            // Llamar a sp_RegistrarPago
        }

        private void Cancelar(object? parameter) { /* Cerrar vista */ }

    }
}
