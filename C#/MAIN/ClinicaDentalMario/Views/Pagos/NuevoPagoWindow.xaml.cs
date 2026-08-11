using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ClinicaDentalMario.Views.Pagos
{
    public partial class NuevoPagoWindow : Window
    {
        private readonly int _idTratamientoPaciente;
        private readonly decimal _saldoPendiente;
        private readonly PagoRepository _pagoRepo;

        public bool PagoRealizado { get; private set; } = false;

        public NuevoPagoWindow(int idTratamientoPaciente, string nombreTratamiento, decimal saldoPendiente)
        {
            InitializeComponent();
            _idTratamientoPaciente = idTratamientoPaciente;
            _saldoPendiente = saldoPendiente;
            _pagoRepo = new PagoRepository();

            TxtTratamientoInfo.Text = $"Tratamiento: {nombreTratamiento}";
            TxtSaldoPendienteInfo.Text = $"Saldo Pendiente Actual: ${_saldoPendiente:N2}";
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Por favor ingresa un monto válido mayor a $0.00", "Monto Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (monto > _saldoPendiente)
            {
                var confirmacion = MessageBox.Show($"El monto (${monto:N2}) supera el saldo pendiente (${_saldoPendiente:N2}). ¿Deseas continuar?", "Advertencia", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmacion != MessageBoxResult.Yes) return;
            }

            try
            {
                string metodo = (CmbMetodoPago.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Efectivo";

                var nuevoPago = new PagoModel
                {
                    IdTratamientoPaciente = _idTratamientoPaciente,
                    Monto = monto,
                    FechaPago = DateTime.Now,
                    MetodoPago = metodo,
                    Observacion = TxtObservacion.Text
                };

                await _pagoRepo.RegistrarPagoAsync(nuevoPago);
                MessageBox.Show("¡Abono registrado con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                PagoRealizado = true;
                DialogResult = true;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar abono: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            
        }
    }
}