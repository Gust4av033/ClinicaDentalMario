namespace ClinicaDentalMario.Navigation
{
    /// <summary>
    /// Contrato mínimo para cambiar la vista mostrada dentro del Shell principal.
    /// </summary>
    public interface INavigationService
    {
        object? VistaActual { get; }

        event EventHandler? VistaActualChanged;

        void Navegar(object vista);
    }
}
