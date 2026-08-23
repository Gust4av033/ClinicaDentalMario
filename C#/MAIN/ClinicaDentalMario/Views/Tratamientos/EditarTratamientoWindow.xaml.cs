using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClinicaDentalMario.Views.Tratamientos
{
    /// <summary>
    /// Lógica de interacción para EditarTratamientoWindow.xaml
    /// </summary>
    public partial class EditarTratamientoWindow : Window
    {
        public EditarTratamientoWindow()
        {
            InitializeComponent();
        }

        // FILTRO PARA PERMITIR SOLO NÚMEROS Y UN PUNTO DECIMAL
        private void ValidarNumerosYDecimales_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite dígitos (0-9) y un punto decimal (.)
            Regex regex = new Regex(@"[^0-9.]+");

            // Validar que no se escriba más de un punto decimal
            if (e.Text == "." && (sender as System.Windows.Controls.TextBox)?.Text.Contains(".") == true)
            {
                e.Handled = true;
                return;
            }

            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
