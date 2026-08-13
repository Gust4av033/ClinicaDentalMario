using ClinicaDentalMario.ViewModel.Pagos; // 🔥 Necesario para llamar al ViewModel
using System.Windows;

namespace ClinicaDentalMario.Views.Pagos
{
    public partial class NuevoPagoWindow : Window
    {
        // Esta propiedad es un "puente" para que la pantalla de atrás (Estado de Cuenta)
        // sepa si el ViewModel logró guardar el pago con éxito.
        public bool PagoRealizado => (DataContext as NuevoPagoViewModel)?.PagoRealizado ?? false;

        public NuevoPagoWindow(int idTratamientoPaciente, string nombreTratamiento, decimal saldoPendiente)
        {
            InitializeComponent();

            // 🔥 AQUÍ CONECTAMOS LA VISTA CON SU CEREBRO (EL VIEWMODEL) 🔥
            // Le pasamos los datos al ViewModel y él se encargará de todo el trabajo sucio.
            this.DataContext = new NuevoPagoViewModel(idTratamientoPaciente, nombreTratamiento, saldoPendiente);
        }
    }
}