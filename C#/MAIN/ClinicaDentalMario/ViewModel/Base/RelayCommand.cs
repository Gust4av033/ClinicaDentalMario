using System.Windows.Input;

namespace ClinicaDentalMario.ViewModel.Base
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(
                _ => execute(),
                canExecute is null ? null : _ => canExecute())
        {
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void NotificarCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Permite solicitar una reevaluación de CanExecute aunque una propiedad de comando
    /// esté expuesta como ICommand. Conserva la abstracción de la vista sin obligar a
    /// convertir cada comando al tipo concreto antes de notificar cambios.
    /// </summary>
    public static class CommandExtensions
    {
        public static void NotificarCanExecuteChanged(this ICommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            switch (command)
            {
                case RelayCommand relayCommand:
                    relayCommand.NotificarCanExecuteChanged();
                    break;
                case AsyncRelayCommand asyncRelayCommand:
                    asyncRelayCommand.NotificarCanExecuteChanged();
                    break;
                default:
                    CommandManager.InvalidateRequerySuggested();
                    break;
            }
        }
    }
}
