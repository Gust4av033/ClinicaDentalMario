using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class EditarUsuarioViewModel : ViewModelBase
    {
        private UsuarioModel _usuarioEditado;
        public UsuarioModel UsuarioEditado
        {
            get => _usuarioEditado;
            set => SetProperty(ref _usuarioEditado, value);
        }

        private ObservableCollection<RolModel> _rolesDisponibles = new();
        public ObservableCollection<RolModel> RolesDisponibles
        {
            get => _rolesDisponibles;
            set => SetProperty(ref _rolesDisponibles, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public EditarUsuarioViewModel(UsuarioModel usuario)
        {
            Titulo = "Editar Usuario del Sistema";
            _usuarioEditado = usuario;

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            // Lógica para ejecutar sp_EditarUsuario
        }

        private void Cancelar(object? parameter) { /* Navegar atrás */ }
    }
}
