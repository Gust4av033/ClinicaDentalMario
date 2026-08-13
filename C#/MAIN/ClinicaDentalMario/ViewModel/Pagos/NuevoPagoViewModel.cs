using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Pagos
{
    public class NuevoPagoViewModel : ViewModelBase
    {
        private readonly PagoRepository _pagoRepo;
        public bool PagoRealizado { get; private set; } = false;

        public string NombreTratamientoTexto { get; }
        public string SaldoPendienteTexto { get; }
        private readonly decimal _saldoMaximo;

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

        public NuevoPagoViewModel(int idTratamiento, string nombreTratamiento, decimal saldoPendiente)
        {
            Titulo = "Registrar Abono";
            _pagoRepo = new PagoRepository();

            _saldoMaximo = saldoPendiente;
            NombreTratamientoTexto = $"Tratamiento: {nombreTratamiento}";
            SaldoPendienteTexto = $"Saldo Pendiente: {saldoPendiente:C}";

            _nuevoPago = new PagoModel
            {
                IdTratamientoPaciente = idTratamiento,
                FechaPago = DateTime.Now,
                MetodoPago = "Efectivo",
                Monto = 0
            };

            GuardarCommand = new RelayCommand(async (param) => await GuardarAsync(param));
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private async Task GuardarAsync(object? parameter)
        {
            MensajeError = string.Empty;

            if (NuevoPago.Monto <= 0)
            {
                MensajeError = "El monto debe ser mayor a cero.";
                return;
            }

            if (NuevoPago.Monto > _saldoMaximo)
            {
                MensajeError = $"No puedes abonar más del saldo pendiente ({_saldoMaximo:C}).";
                return;
            }

            try
            {
                // Guarda en la base de datos
                await _pagoRepo.RegistrarPagoAsync(NuevoPago);

                PagoRealizado = true;
                MessageBox.Show($"Abono de {NuevoPago.Monto:C} registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Cierra la ventana
                if (parameter is Window ventana)
                {
                    ventana.DialogResult = true;
                    ventana.Close();
                }
            }
            catch (Exception ex)
            {
                MensajeError = "Error al guardar el abono: " + ex.Message;
            }
        }

        private void Cancelar(object? parameter)
        {
            if (parameter is Window ventana)
            {
                ventana.DialogResult = false;
                ventana.Close();
            }
        }
    }
}