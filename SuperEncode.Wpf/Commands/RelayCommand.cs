using System.Windows.Input;

namespace SuperEncode.Wpf.Commands
{
    public class RelayCommand<T>(Action<T> execute, Func<T?, bool>? canExecute = null) : ICommand
    {

        public bool CanExecute(object? parameter)
        {
            return canExecute is null || canExecute.Invoke((T)parameter!);
        }

        public void Execute(object? parameter)
        {
            execute.Invoke((T)parameter!);
        
        }
        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? CanExecuteChanged;
    }
}
