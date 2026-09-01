namespace ClinicaDentalMario.Navigation
{
    /// <summary>
    /// Crea las vistas principales que puede mostrar el Shell.
    /// Mantiene a MainViewModel desacoplado de las clases concretas de WPF.
    /// </summary>
    public interface IShellViewFactory
    {
        object CrearDashboard(Action<object> navegar);
        object CrearPacientes(Action<object> navegar);
        object CrearAgenda(Action<object> navegar);
        object CrearTratamientos(Action<object> navegar);
        object CrearPagos(Action<object> navegar);
        object CrearReportes(Action<object> navegar);
        object CrearConfiguracion();
    }
}
