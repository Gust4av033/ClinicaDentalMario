using ClinicaDentalMario.ViewModel.Base;
using System.Windows;

namespace ClinicaDentalMario.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var viewModel = new MainViewModel();
            viewModel.CierreSesionSolicitado += OnCierreSesionSolicitado;
            DataContext = viewModel;
        }

        private void OnCierreSesionSolicitado(object? sender, EventArgs e)
        {
            if (Application.Current is not App app)
            {
                return;
            }

            app.MostrarLogin();
            Close();
        }
    }
}
