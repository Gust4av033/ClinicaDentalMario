using ClinicaDentalMario.ViewModel.Configuracion;
using System.Windows.Controls;

namespace ClinicaDentalMario.Views.Configuracion
{
    public partial class BitacoraView : UserControl
    {
        public BitacoraView()
        {
            InitializeComponent();
            DataContext = new BitacoraViewModel();
        }
    }
}
