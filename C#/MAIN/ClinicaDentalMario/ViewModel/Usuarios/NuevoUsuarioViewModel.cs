using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class NuevoUsuarioViewModel : ViewModelBase
    {
        private UsuarioModel _nuevoUsuario;
        public UsuarioModel NuevoUsuario
        {
            get => _nuevoUsuario;
            set => SetProperty(ref _nuevoUsuario, value);
        }

        // Lista para llenar el ComboBox de Roles (Admin, Doctor, Recepcionista)
        private ObservableCollection<RolModel> _rolesDisponibles = new();
        public ObservableCollection<RolModel> RolesDisponibles
        {
            get => _rolesDisponibles;
            set => SetProperty(ref _rolesDisponibles, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public NuevoUsuarioViewModel()
        {
            Titulo = "Crear Nuevo Usuario";
            _nuevoUsuario = new UsuarioModel { Activo = true };

            // Aquí llamarías al RolRepository para llenar _rolesDisponibles

            GuardarCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter) { /* Logica de guardado */ }
        private void Cancelar(object? parameter) { /* Cancelar */ }
    }
}
