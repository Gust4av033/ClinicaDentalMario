using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class EditarTratamientoViewModel : ViewModelBase
    {
        private readonly TratamientoRepository _tratamientoRepo;
        public int IdTratamientoPaciente { get; }
        public string NombreTratamientoTexto { get; }

        public bool FueActualizado { get; private set; } = false;

        private decimal _costoTotal;
        public decimal CostoTotal
        {
            get => _costoTotal;
            set => SetProperty(ref _costoTotal, value);
        }

        private string _observaciones = string.Empty;
        public string Observaciones
        {
            get => _observaciones;
            set => SetProperty(ref _observaciones, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public EditarTratamientoViewModel(int idTratamientoPaciente, string nombreTratamiento, decimal costo, string observaciones)
        {
            _tratamientoRepo = new TratamientoRepository();

            // Cargamos los datos actuales
            IdTratamientoPaciente = idTratamientoPaciente;
            NombreTratamientoTexto = $"Procedimiento: {nombreTratamiento}";
            CostoTotal = costo;
            Observaciones = observaciones ?? string.Empty;

            GuardarCommand = new RelayCommand(async (param) => await GuardarAsync(param));
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private async Task GuardarAsync(object? parameter)
        {
            if (CostoTotal < 0)
            {
                MensajeError = "El costo no puede ser un número negativo.";
                return;
            }

            try
            {
                // 🔥 Asegúrate de tener este método en tu TratamientoRepository
                await _tratamientoRepo.ActualizarTratamientoAsync(IdTratamientoPaciente, CostoTotal, Observaciones);

                FueActualizado = true;
                MessageBox.Show("Tratamiento actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                Cancelar(parameter); // Cierra la ventana
            }
            catch (Exception ex)
            {
                MensajeError = "Error al actualizar: " + ex.Message;
            }
        }

        private void Cancelar(object? parameter)
        {
            if (parameter is Window ventana)
            {
                if (FueActualizado) ventana.DialogResult = true;
                ventana.Close();
            }
        }
    }
}