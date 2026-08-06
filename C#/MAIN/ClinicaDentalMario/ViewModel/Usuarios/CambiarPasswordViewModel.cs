using ClinicaDentalMario.Models;
using ClinicaDentalMario.ViewModel.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace ClinicaDentalMario.ViewModel.Usuarios
{
    public class CambiarPasswordViewModel : ViewModelBase
    {
        private UsuarioModel _usuarioContexto;
        public UsuarioModel UsuarioContexto
        {
            get => _usuarioContexto;
            set => SetProperty(ref _usuarioContexto, value);
        }

        // Nota: En WPF no enlazamos passwords a strings por seguridad,
        // pero lo manejamos a través del CommandParameter en la Vista.

        public ICommand GuardarNuevaPasswordCommand { get; }
        public ICommand CancelarCommand { get; }

        public CambiarPasswordViewModel(UsuarioModel usuario)
        {
            Titulo = $"Cambiar Clave: {usuario.NombreUsuario}";
            _usuarioContexto = usuario;

            GuardarNuevaPasswordCommand = new RelayCommand(Guardar);
            CancelarCommand = new RelayCommand(Cancelar);
        }

        private void Guardar(object? parameter)
        {
            // Ejecutar sp_CambiarPassword (hasheando la contraseña antes)
        }

        private void Cancelar(object? parameter) { /* Cerrar */ }
    }
}
