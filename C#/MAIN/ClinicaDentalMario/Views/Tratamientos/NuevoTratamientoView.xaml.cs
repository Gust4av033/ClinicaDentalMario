using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicaDentalMario.Views.Tratamientos
{
    /// <summary>
    /// Lógica de interacción para NuevoTratamientoView.xaml
    /// </summary>
    public partial class NuevoTratamientoView : UserControl
    {
        public NuevoTratamientoView()
        {
            InitializeComponent();
        }

        // FILTRO PARA PERMITIR SOLO NÚMEROS Y UN PUNTO DECIMAL
        private void ValidarNumerosYDecimales_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9.]+");

            // Validar que no se escriba más de un punto decimal
            if (e.Text == "." && (sender as TextBox)?.Text.Contains(".") == true)
            {
                e.Handled = true;
                return;
            }

            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
