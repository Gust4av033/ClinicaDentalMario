using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class UsuariosViewModel : ViewModelBase
    {
        private ObservableCollection<UsuarioModel> _usuarios = new();
        public ObservableCollection<UsuarioModel> Usuarios
        {
            get => _usuarios;
            set => SetProperty(ref _usuarios, value);
        }

        private UsuarioModel? _usuarioSeleccionado;
        public UsuarioModel? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set => SetProperty(ref _usuarioSeleccionado, value);
        }

        public ICommand NuevoUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }
        public ICommand CambiarPasswordCommand { get; }

        public UsuariosViewModel()
        {
            Titulo = "Control de Usuarios del Sistema";

            NuevoUsuarioCommand = new RelayCommand(Nuevo);
            EditarUsuarioCommand = new RelayCommand(Editar, (p) => UsuarioSeleccionado != null);
            CambiarPasswordCommand = new RelayCommand(CambiarPass, (p) => UsuarioSeleccionado != null);
        }

        private void Nuevo(object? parameter) { /* Ir a NuevoUsuarioViewModel */ }
        private void Editar(object? parameter) { /* Ir a EditarUsuarioViewModel */ }
        private void CambiarPass(object? parameter) { /* Ir a CambiarPasswordViewModel */ }
    }
}
