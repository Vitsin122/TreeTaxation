using NetTopologySuite.Algorithm.Construct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TreeTaxation
{
    public class RelayCommand : ICommand
    {
        private Action<object?> execute;
        private Func<object, bool> canExecute;
        private Action _execute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action _execute, Action<object?> execute = null, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            this._execute = _execute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            this._execute();
        }
    }
}
