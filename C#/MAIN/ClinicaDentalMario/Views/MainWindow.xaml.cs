using ClinicaDentalMario.ViewModel.Base;
using System.Windows;

namespace ClinicaDentalMario.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
            : this(new MainViewModel())
        {
        }

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _viewModel.CierreSesionSolicitado += OnCierreSesionSolicitado;
            DataContext = _viewModel;

            Closed += OnClosed;
        }

        private void OnCierreSesionSolicitado(object? sender, EventArgs e)
        {
            if (Application.Current is not App app)
            {
                Application.Current.Shutdown();
                return;
            }

            app.MostrarLogin();
            Close();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _viewModel.CierreSesionSolicitado -= OnCierreSesionSolicitado;
            Closed -= OnClosed;
        }
    }
}
