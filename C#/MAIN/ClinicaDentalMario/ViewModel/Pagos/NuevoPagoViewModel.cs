using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.Services;
using ClinicaDentalMario.ViewModel.Base;
using System.Globalization;
using System.Windows;

namespace ClinicaDentalMario.ViewModel.Pagos
{
    public class NuevoPagoViewModel : ViewModelBase
    {
        private readonly PagoRepository _pagoRepository;
        private readonly IMessageService _messageService;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly int _idTratamientoPaciente;

        public bool PagoRealizado { get; private set; }
        public string NombreTratamientoTexto { get; }

        public IReadOnlyList<string> MetodosPago { get; } = new[]
        {
            "Efectivo",
            "Tarjeta de Débito/Crédito",
            "Transferencia Bancaria",
            "Tigo Money / Otro"
        };

        private decimal _saldoPendiente;
        public decimal SaldoPendiente
        {
            get => _saldoPendiente;
            private set
            {
                if (SetProperty(ref _saldoPendiente, Math.Max(0m, value)))
                    OnPropertyChanged(nameof(SaldoPendienteTexto));
            }
        }

        public string SaldoPendienteTexto => $"Saldo pendiente: {SaldoPendiente:C}";

        private string _montoTexto = string.Empty;
        public string MontoTexto
        {
            get => _montoTexto;
            set
            {
                if (SetProperty(ref _montoTexto, value ?? string.Empty))
                {
                    MensajeError = string.Empty;
                    GuardarCommand.NotificarCanExecuteChanged();
                }
            }
        }

        private string _metodoPagoSeleccionado = "Efectivo";
        public string MetodoPagoSeleccionado
        {
            get => _metodoPagoSeleccionado;
            set
            {
                if (SetProperty(ref _metodoPagoSeleccionado, value ?? string.Empty))
                {
                    MensajeError = string.Empty;
                    GuardarCommand.NotificarCanExecuteChanged();
                }
            }
        }

        private string _observacion = string.Empty;
        public string Observacion
        {
            get => _observacion;
            set => SetProperty(ref _observacion, value ?? string.Empty);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public AsyncRelayCommand GuardarCommand { get; }
        public RelayCommand UsarSaldoTotalCommand { get; }
        public RelayCommand CancelarCommand { get; }

        public NuevoPagoViewModel(int idTratamientoPaciente, string nombreTratamiento, decimal saldoPendiente)
            : this(
                idTratamientoPaciente,
                nombreTratamiento,
                saldoPendiente,
                new PagoRepository(),
                new MessageService(),
                new ExceptionHandler(new MessageService()))
        {
        }

        public NuevoPagoViewModel(
            int idTratamientoPaciente,
            string nombreTratamiento,
            decimal saldoPendiente,
            PagoRepository pagoRepository,
            IMessageService messageService,
            IExceptionHandler exceptionHandler)
        {
            if (idTratamientoPaciente <= 0)
                throw new ArgumentOutOfRangeException(nameof(idTratamientoPaciente));

            _idTratamientoPaciente = idTratamientoPaciente;
            _pagoRepository = pagoRepository ?? throw new ArgumentNullException(nameof(pagoRepository));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            Titulo = "Registrar Abono";
            NombreTratamientoTexto = string.IsNullOrWhiteSpace(nombreTratamiento)
                ? "Tratamiento dental"
                : nombreTratamiento.Trim();
            SaldoPendiente = saldoPendiente;

            GuardarCommand = new AsyncRelayCommand(
                GuardarAsync,
                _ => !EstaCargando && SaldoPendiente > 0m && !string.IsNullOrWhiteSpace(MontoTexto));

            UsarSaldoTotalCommand = new RelayCommand(
                _ => UsarSaldoTotal(),
                _ => !EstaCargando && SaldoPendiente > 0m);

            CancelarCommand = new RelayCommand(
                Cancelar,
                _ => !EstaCargando);
        }

        private void UsarSaldoTotal()
        {
            MontoTexto = SaldoPendiente.ToString("0.00", CultureInfo.CurrentCulture);
        }

        private async Task GuardarAsync(object? parameter)
        {
            MensajeError = string.Empty;

            if (!TryObtenerMonto(out decimal monto))
            {
                MensajeError = "Ingresa un monto válido.";
                return;
            }

            if (monto <= 0m)
            {
                MensajeError = "El monto debe ser mayor a cero.";
                return;
            }

            if (monto > SaldoPendiente)
            {
                MensajeError = $"El monto no puede superar el saldo pendiente ({SaldoPendiente:C}).";
                return;
            }

            string metodo = MetodoPagoSeleccionado.Trim();
            if (string.IsNullOrWhiteSpace(metodo))
            {
                MensajeError = "Selecciona un método de pago.";
                return;
            }

            if (metodo.Length > 50)
            {
                MensajeError = "El método de pago es demasiado largo.";
                return;
            }

            string observacion = Observacion.Trim();
            if (observacion.Length > 255)
            {
                MensajeError = "La observación no puede superar 255 caracteres.";
                return;
            }

            EstaCargando = true;
            NotificarComandos();

            try
            {
                var pago = new PagoModel
                {
                    IdTratamientoPaciente = _idTratamientoPaciente,
                    FechaPago = DateTime.Now,
                    Monto = decimal.Round(monto, 2, MidpointRounding.AwayFromZero),
                    MetodoPago = metodo,
                    Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion
                };

                var resultado = await _pagoRepository.RegistrarPagoValidadoAsync(pago);
                SaldoPendiente = resultado.SaldoAntes;

                if (!resultado.Registrado)
                {
                    MensajeError = ObtenerMensajeRechazo(resultado.EstadoTratamiento, resultado.SaldoAntes);
                    return;
                }

                SaldoPendiente = Math.Max(0m, resultado.SaldoAntes - pago.Monto);
                PagoRealizado = true;

                _messageService.MostrarExito(
                    $"Abono de {pago.Monto:C} registrado correctamente.",
                    "Abono registrado");

                if (parameter is Window ventana)
                {
                    ventana.DialogResult = true;
                    ventana.Close();
                }
            }
            catch (Exception ex)
            {
                MensajeError = _exceptionHandler.ObtenerMensajeUsuario(
                    ex,
                    "No fue posible registrar el abono.");
            }
            finally
            {
                EstaCargando = false;
                NotificarComandos();
            }
        }

        private string ObtenerMensajeRechazo(string? estado, decimal saldo)
        {
            if (string.Equals(estado, "Finalizado", StringComparison.OrdinalIgnoreCase))
                return "El tratamiento ya está finalizado y no admite nuevos abonos.";

            if (string.Equals(estado, "Cancelado", StringComparison.OrdinalIgnoreCase))
                return "El tratamiento está cancelado y no admite nuevos abonos.";

            if (saldo <= 0m)
                return "Este tratamiento ya no tiene saldo pendiente.";

            return $"El saldo cambió mientras registrabas el abono. El saldo actual es {saldo:C}. Revisa el monto e inténtalo nuevamente.";
        }

        private bool TryObtenerMonto(out decimal monto)
        {
            string texto = MontoTexto.Trim();

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                    CultureInfo.CurrentCulture,
                    out monto))
            {
                return true;
            }

            return decimal.TryParse(
                texto,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out monto);
        }

        private void Cancelar(object? parameter)
        {
            if (parameter is Window ventana)
            {
                ventana.DialogResult = false;
                ventana.Close();
            }
        }

        private void NotificarComandos()
        {
            GuardarCommand.NotificarCanExecuteChanged();
            UsarSaldoTotalCommand.NotificarCanExecuteChanged();
            CancelarCommand.NotificarCanExecuteChanged();
        }
    }
}
