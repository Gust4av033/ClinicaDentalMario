using ClinicaDentalMario.Models;
using ClinicaDentalMario.Repositories;
using ClinicaDentalMario.ViewModel.Base;
using ClinicaDentalMario.Views;
using ClinicaDentalMario.Views.Usuarios;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class UsuariosViewModel : ViewModelBase
    {
        private readonly UsuarioRepository _usuarioRepo;

        private ObservableCollection<UsuarioModel> _usuarios = new();
        public ObservableCollection<UsuarioModel> Usuarios { get => _usuarios; set => SetProperty(ref _usuarios, value); }

        public ICommand CargarUsuariosCommand { get; }
        public ICommand NuevoUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }

        public UsuariosViewModel()
        {
            Titulo = "Gestión de Usuarios y Personal";
            _usuarioRepo = new UsuarioRepository();

            CargarUsuariosCommand = new RelayCommand(async p => await CargarUsuariosAsync());
            NuevoUsuarioCommand = new RelayCommand(NuevoUsuario);
            EditarUsuarioCommand = new RelayCommand(EditarUsuario);

            _ = CargarUsuariosAsync();
        }

        public async Task CargarUsuariosAsync()
        {
            EstaCargando = true;
            try
            {
                var lista = await _usuarioRepo.ListarUsuariosAsync();
                Usuarios = new ObservableCollection<UsuarioModel>(lista);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void NuevoUsuario(object? parameter)
        {
            // 🔥 SEGURIDAD: Solo Administradores pueden crear personal
            if (ClinicaDentalMario.Common.UsuarioActual.NombreRol != "Administrador")
            {
                MessageBox.Show("ACCESO DENEGADO: Solo los Administradores tienen permisos para registrar nuevo personal en el sistema.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vm = new NuevoEditarUsuarioViewModel();
                var modal = new NuevoEditarUsuarioWindow
                {
                    DataContext = vm,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var mainWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
                if (mainWindow != null) modal.Owner = mainWindow;

                if (modal.ShowDialog() == true && vm.UsuarioGuardado)
                {
                    _ = CargarUsuariosAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al abrir la ventana: {ex.Message}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditarUsuario(object? parameter)
        {
            // 🔥 SEGURIDAD: Solo Administradores pueden editar al personal
            if (ClinicaDentalMario.Common.UsuarioActual.NombreRol != "Administrador")
            {
                MessageBox.Show("ACCESO DENEGADO: No tienes permisos para modificar las credenciales o roles de otros usuarios.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (parameter is UsuarioModel usuarioSeleccionado)
            {
                try
                {
                    var vm = new NuevoEditarUsuarioViewModel(usuarioSeleccionado);
                    var modal = new NuevoEditarUsuarioWindow
                    {
                        DataContext = vm,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    var mainWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
                    if (mainWindow != null) modal.Owner = mainWindow;

                    if (modal.ShowDialog() == true && vm.UsuarioGuardado)
                    {
                        _ = CargarUsuariosAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al abrir la ventana: {ex.Message}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}