using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClinicaDentalMario.ViewModel.Base
{
    /// <summary>
    /// Base para ViewModels con validación de campos mediante INotifyDataErrorInfo.
    /// Permite que WPF muestre errores de binding y que los comandos consulten HasErrors.
    /// </summary>
    public abstract class ValidatableViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errores = new();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errores.Any(x => x.Value.Count > 0);

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return _errores.SelectMany(x => x.Value).ToList();
            }

            return _errores.TryGetValue(propertyName, out var errores)
                ? errores
                : Enumerable.Empty<string>();
        }

        protected void EstablecerErrores(
            IEnumerable<string> errores,
            [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var lista = errores
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (lista.Count == 0)
            {
                LimpiarErrores(propertyName);
                return;
            }

            _errores[propertyName] = lista;
            NotificarCambioErrores(propertyName);
        }

        protected void AgregarError(
            string error,
            [CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(error))
            {
                return;
            }

            if (!_errores.TryGetValue(propertyName, out var lista))
            {
                lista = new List<string>();
                _errores[propertyName] = lista;
            }

            if (!lista.Contains(error))
            {
                lista.Add(error);
                NotificarCambioErrores(propertyName);
            }
        }

        protected void LimpiarErrores([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            if (_errores.Remove(propertyName))
            {
                NotificarCambioErrores(propertyName);
            }
        }

        protected void LimpiarTodosLosErrores()
        {
            var propiedades = _errores.Keys.ToList();
            _errores.Clear();

            foreach (var propiedad in propiedades)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propiedad));
            }

            OnPropertyChanged(nameof(HasErrors));
        }

        protected bool ValidarCampo(
            IEnumerable<string> errores,
            [CallerMemberName] string? propertyName = null)
        {
            EstablecerErrores(errores, propertyName);
            return string.IsNullOrWhiteSpace(propertyName) || !_errores.ContainsKey(propertyName);
        }

        private void NotificarCambioErrores(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
