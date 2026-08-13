using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClinicaDentalMario.Views.Configuracion
{
    /// <summary>
    /// Lógica de interacción para BitacoraView.xaml
    /// </summary>
    public partial class BitacoraView : UserControl
    {
        public BitacoraView()
        {
            InitializeComponent();
            // ¡Esto es vital para que la tabla se llene!
            this.DataContext = new ClinicaDentalMario.ViewModel.Configuracion.BitacoraViewModel();
        }
    }
}
