using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Windows;

namespace ClinicaDentalMario.ViewModel.Tratamientos
{
    public class EditarTratamientoViewModel : ViewModelBase
    {
        private readonly TratamientoRepository _tratamientoRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;

        public int IdTratamientoPaciente { get; }
        public string NombreTratamientoTexto { get; }
        public bool FueActualizado { get; private set; }

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
            set => SetProperty(ref _observaciones, value ?? string.Empty);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand GuardarCommand { get; }
        public RelayCommand CancelarCommand { get; }

        public EditarTratamientoViewModel(
            int idTratamientoPaciente,
            string nombreTratamiento,
            decimal costo,
            string? observaciones)
            : this(
                idTratamientoPaciente,
                nombreTratamiento,
                costo,
                observaciones,
                new TratamientoRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public EditarTratamientoViewModel(
            int idTratamientoPaciente,
            string nombreTratamiento,
            decimal costo,
            string? observaciones,
            TratamientoRepository tratamientoRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            if (idTratamientoPaciente <= 0)
                throw new ArgumentOutOfRangeException(nameof(idTratamientoPaciente));

            _tratamientoRepository = tratamientoRepository ?? throw new ArgumentNullException(nameof(tratamientoRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            IdTratamientoPaciente = idTratamientoPaciente;
            NombreTratamientoTexto = $"Procedimiento: {nombreTratamiento}";
            CostoTotal = costo;
            Observaciones = observaciones ?? string.Empty;
            Titulo = "Editar Tratamiento";

            GuardarCommand = new AsyncRelayCommand(GuardarAsync);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private async Task GuardarAsync(object? parameter)
        {
            MensajeError = string.Empty;

            if (CostoTotal < 0)
            {
                MensajeError = "El costo no puede ser un número negativo.";
                return;
            }

            EstaCargando = true;

            try
            {
                await _tratamientoRepository.ActualizarTratamientoAsync(
                    IdTratamientoPaciente,
                    CostoTotal,
                    Observaciones);

                FueActualizado = true;
                _messageService.MostrarExito("Tratamiento actualizado correctamente.");
                CerrarVentana(parameter, true);
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible actualizar el tratamiento.");
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void Cancelar(object? parameter)
        {
            CerrarVentana(parameter, FueActualizado);
        }

        private static void CerrarVentana(object? parameter, bool resultado)
        {
            if (parameter is not Window ventana)
                return;

            if (resultado)
            {
                ventana.DialogResult = true;
                return;
            }

            ventana.Close();
        }
    }
}
