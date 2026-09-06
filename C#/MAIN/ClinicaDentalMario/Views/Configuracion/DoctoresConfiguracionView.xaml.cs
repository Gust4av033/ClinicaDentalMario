using ClinicaDentalMario.ViewModel.Configuracion;
using System.Windows.Controls;

namespace ClinicaDentalMario.Views.Configuracion
{
    public partial class DoctoresConfiguracionView : UserControl
    {
        public DoctoresConfiguracionView()
        {
            InitializeComponent();
            DataContext = new DoctoresConfiguracionViewModel();
        }
    }
}
