using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace ClinicaDentalMario.Services
{
    /// <summary>
    /// Manejo central de excepciones de la aplicación.
    /// No expone detalles técnicos al usuario y deja un único punto para incorporar logging más adelante.
    /// </summary>
    public sealed class ExceptionHandler : IExceptionHandler
    {
        private readonly IMessageService _messageService;

        public ExceptionHandler(IMessageService messageService)
        {
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public string ObtenerMensajeUsuario(Exception exception, string? contexto = null)
        {
            ArgumentNullException.ThrowIfNull(exception);

            string mensaje = exception switch
            {
                SqlException => "No fue posible comunicarse correctamente con la base de datos. Verifica la conexión e inténtalo nuevamente.",
                TimeoutException => "La operación tardó demasiado tiempo. Inténtalo nuevamente.",
                IOException => "Ocurrió un problema al acceder a un archivo requerido por el sistema.",
                SocketException => "No fue posible completar la comunicación requerida por el sistema.",
                UnauthorizedAccessException => "No tienes permisos suficientes para completar esta operación.",
                ArgumentException => "Uno de los datos proporcionados no es válido.",
                InvalidOperationException => "La operación no puede realizarse en el estado actual.",
                _ => "Ocurrió un error inesperado. Inténtalo nuevamente."
            };

            return string.IsNullOrWhiteSpace(contexto)
                ? mensaje
                : $"{contexto} {mensaje}";
        }

        public void Manejar(Exception exception, string? contexto = null)
        {
            ArgumentNullException.ThrowIfNull(exception);

            // Deja disponible el detalle técnico durante desarrollo sin mostrárselo al usuario.
            Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexto ?? "Error no controlado"}: {exception}");

            _messageService.MostrarError(ObtenerMensajeUsuario(exception, contexto));
        }
    }
}
