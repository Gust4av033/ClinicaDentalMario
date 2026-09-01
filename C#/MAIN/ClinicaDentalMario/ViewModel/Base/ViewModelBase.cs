using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClinicaDentalMario.ViewModel.Base
{
    /// <summary>
    /// Base común para todos los ViewModels de la aplicación.
    /// Centraliza notificación de propiedades, estado de carga y mensajes al usuario.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            protected set => SetProperty(ref _estaCargando, value);
        }

        private string _titulo = string.Empty;
        public string Titulo
        {
            get => _titulo;
            protected set => SetProperty(ref _titulo, value);
        }

        private string _mensajeUsuario = string.Empty;
        public string MensajeUsuario
        {
            get => _mensajeUsuario;
            protected set
            {
                if (SetProperty(ref _mensajeUsuario, value))
                {
                    OnPropertyChanged(nameof(TieneMensaje));
                }
            }
        }

        private TipoMensaje _tipoMensaje = TipoMensaje.Ninguno;
        public TipoMensaje TipoMensaje
        {
            get => _tipoMensaje;
            protected set => SetProperty(ref _tipoMensaje, value);
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(MensajeUsuario);

        protected void MostrarMensaje(string mensaje, TipoMensaje tipo = TipoMensaje.Informacion)
        {
            TipoMensaje = tipo;
            MensajeUsuario = mensaje;
        }

        protected void MostrarExito(string mensaje) => MostrarMensaje(mensaje, TipoMensaje.Exito);

        protected void MostrarAdvertencia(string mensaje) => MostrarMensaje(mensaje, TipoMensaje.Advertencia);

        protected void MostrarError(string mensaje) => MostrarMensaje(mensaje, TipoMensaje.Error);

        protected void LimpiarMensaje()
        {
            MensajeUsuario = string.Empty;
            TipoMensaje = TipoMensaje.Ninguno;
        }

        /// <summary>
        /// Ejecuta una operación asíncrona controlando automáticamente EstaCargando.
        /// Las excepciones se propagan para que cada módulo decida el mensaje apropiado.
        /// </summary>
        protected async Task EjecutarConCargaAsync(Func<Task> accion)
        {
            if (EstaCargando)
            {
                return;
            }

            EstaCargando = true;

            try
            {
                await accion();
            }
            finally
            {
                EstaCargando = false;
            }
        }
    }
}
