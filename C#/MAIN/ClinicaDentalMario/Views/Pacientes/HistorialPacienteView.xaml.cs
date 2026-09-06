using ClinicaDentalMario.ViewModel.Pacientes;
using System.Windows;
using System.Windows.Controls;

namespace ClinicaDentalMario.Views.Pacientes
{
    /// <summary>
    /// Lógica de interacción exclusivamente visual del expediente clínico.
    /// </summary>
    public partial class HistorialPacienteView : UserControl
    {
        public HistorialPacienteView()
        {
            InitializeComponent();
        }

        private void AbrirOdontogramaCompleto_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not HistorialPacienteViewModel vm)
            {
                return;
            }

            var ventana = new OdontogramaWindow
            {
                DataContext = vm.OdontogramaVM,
                Title = $"Odontograma Digital - {vm.PacienteActual.NombreCompleto}",
                Owner = Window.GetWindow(this)
            };

            ventana.ShowDialog();
        }
    }
}
