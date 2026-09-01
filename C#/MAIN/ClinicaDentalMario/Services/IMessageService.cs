namespace ClinicaDentalMario.Services
{
    /// <summary>
    /// Contrato común para mensajes y confirmaciones mostrados al usuario.
    /// Permite que los ViewModels no dependan directamente de MessageBox.
    /// </summary>
    public interface IMessageService
    {
        void MostrarInformacion(string mensaje, string titulo = "Información");

        void MostrarExito(string mensaje, string titulo = "Operación completada");

        void MostrarAdvertencia(string mensaje, string titulo = "Advertencia");

        void MostrarError(string mensaje, string titulo = "Error");

        bool Confirmar(string mensaje, string titulo = "Confirmación");
    }
}
