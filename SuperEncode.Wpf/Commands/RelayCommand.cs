using System.Windows.Input;

namespace SuperEncode.Wpf.Commands
{
    class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {

        public bool CanExecute(object? parameter)
        {
            return canExecute != null && canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute.Invoke(parameter);
        
        }
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? CanExecuteChanged;
    }
}
