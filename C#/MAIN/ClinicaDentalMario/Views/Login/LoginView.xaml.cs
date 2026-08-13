using System.Windows;

namespace ClinicaDentalMario.Views.Login
{
    /// <summary>
    /// Lógica de interacción para LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            this.DataContext = new ClinicaDentalMario.ViewModel.Login.LoginViewModel(); // <--- ESTA LÍNEA
        }
    }
}
