using System.Windows;

namespace ClinicaDentalMario.Services
{
    /// <summary>
    /// Implementación WPF de los mensajes y confirmaciones del sistema.
    /// Centraliza el uso de MessageBox para evitar repetirlo en los ViewModels.
    /// </summary>
    public sealed class MessageService : IMessageService
    {
        public void MostrarInformacion(string mensaje, string titulo = "Información")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void MostrarExito(string mensaje, string titulo = "Operación completada")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void MostrarAdvertencia(string mensaje, string titulo = "Advertencia")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void MostrarError(string mensaje, string titulo = "Error")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool Confirmar(string mensaje, string titulo = "Confirmación")
        {
            return MessageBox.Show(
                mensaje,
                titulo,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
