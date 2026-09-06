using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ClinicaDentalMario.Services
{
    /// <summary>
    /// Manejo central de excepciones de la aplicación.
    /// No expone detalles técnicos al usuario y conserva un log local para diagnóstico.
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

            string detalle = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexto ?? "Error no controlado"}: {exception}";
            Debug.WriteLine(detalle);
            RegistrarEnArchivo(detalle);

            _messageService.MostrarError(ObtenerMensajeUsuario(exception, contexto));
        }

        private static void RegistrarEnArchivo(string detalle)
        {
            try
            {
                string carpeta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CDMario",
                    "Logs");

                Directory.CreateDirectory(carpeta);
                string archivo = Path.Combine(carpeta, "errors.log");

                File.AppendAllText(
                    archivo,
                    detalle + Environment.NewLine + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch
            {
                // Un fallo al escribir el log nunca debe interferir con la aplicación.
            }
        }
    }
}
