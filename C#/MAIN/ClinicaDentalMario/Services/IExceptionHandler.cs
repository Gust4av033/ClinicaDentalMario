namespace ClinicaDentalMario.Services
{
    /// <summary>
    /// Contrato común para transformar excepciones técnicas en mensajes seguros para el usuario.
    /// </summary>
    public interface IExceptionHandler
    {
        string ObtenerMensajeUsuario(Exception exception, string? contexto = null);

        void Manejar(Exception exception, string? contexto = null);
    }
}
