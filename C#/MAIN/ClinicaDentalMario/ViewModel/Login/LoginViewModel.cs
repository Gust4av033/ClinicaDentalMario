using ClinicaDentalMario.ViewModel.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Login
{
    public partial class LoginViewModel : ViewModelBase
    {
        private string _usuario = string.Empty;
        public string Usuario
        {
            get => _usuario;
            set => SetProperty(ref _usuario, value);
        }

        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        // Declaración manual del comando
        public ICommand IniciarSesionCommand { get; }

        public LoginViewModel()
        {
            Titulo = "Iniciar Sesión";

            // Inicializamos el comando apuntando al método asíncrono
            IniciarSesionCommand = new RelayCommand(async (param) => await IniciarSesionAsync(param));
        }

        private async Task IniciarSesionAsync(object? passwordObj)
        {
            if (string.IsNullOrWhiteSpace(Usuario))
            {
                MensajeError = "Por favor, ingresa tu usuario.";
                return;
            }

            EstaCargando = true;
            MensajeError = string.Empty;

            // Simulación de espera a SQL Server
            await Task.Delay(1000);

            EstaCargando = false;
        }
    }
}
