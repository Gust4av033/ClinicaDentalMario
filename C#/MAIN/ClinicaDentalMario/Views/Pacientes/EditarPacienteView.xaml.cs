using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicaDentalMario.Views.Pacientes
{
    /// <summary>
    /// Lógica de interacción para EditarPacienteView.xaml
    /// </summary>
    public partial class EditarPacienteView : UserControl
    {
        public EditarPacienteView()
        {
            InitializeComponent();
        }

        // FILTRO PARA EVITAR LETRAS
        private void ValidarNumeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9-]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
