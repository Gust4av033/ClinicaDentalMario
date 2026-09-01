namespace ClinicaDentalMario.Navigation
{
    /// <summary>
    /// Mantiene la vista actual del Shell y notifica cuando cambia.
    /// No conoce módulos ni crea vistas: solo administra el cambio de contenido.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private object? _vistaActual;

        public object? VistaActual => _vistaActual;

        public event EventHandler? VistaActualChanged;

        public void Navegar(object vista)
        {
            ArgumentNullException.ThrowIfNull(vista);

            if (ReferenceEquals(_vistaActual, vista))
            {
                return;
            }

            _vistaActual = vista;
            VistaActualChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
