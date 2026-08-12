using ClinicaDentalMario.ViewModel.Base;
using System.Windows;

namespace ClinicaDentalMario.Views
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Asignamos el MainViewModel como el DataContext principal de esta ventana
            DataContext = new MainViewModel();
        }
    }
}
