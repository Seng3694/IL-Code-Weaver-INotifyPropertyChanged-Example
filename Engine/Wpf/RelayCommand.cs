using System;
using System.Windows.Input;

namespace Engine.Wpf
{
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        
        private Action<object> _action;
        private Func<object, bool> _canExecute;

        public RelayCommand(Action<object> action, Func<object, bool> canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }
    }
}
