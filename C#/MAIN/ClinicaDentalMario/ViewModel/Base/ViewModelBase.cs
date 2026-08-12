using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClinicaDentalMario.ViewModel.Base
{

    // Hereda de ObservableObject para la notificación de cambios en WPF
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Este método actualiza la variable y avisa a WPF automáticamente
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // --- Propiedades Globales ---

        private bool _estaCargando;
        public bool EstaCargando
        {
            get => _estaCargando;
            set => SetProperty(ref _estaCargando, value);
        }

        private string _titulo = string.Empty;
        public string Titulo
        {
            get => _titulo;
            set => SetProperty(ref _titulo, value);
        }
    }
}
