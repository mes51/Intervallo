using Intervallo.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Intervallo.Command
{
    public abstract class CommandBase : ICommand
    {
        public CommandBase(MainWindow window)
        {
            Window = window;
        }

        event EventHandler InternalCanExecuteChanged;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                InternalCanExecuteChanged += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                InternalCanExecuteChanged -= value;
            }
        }

        protected MainWindow Window { get; }

        public virtual bool CanExecute(object parameter)
        {
            return !Window.Lock;
        }

        public abstract void Execute(object parameter);

        // XXX: change access level to protected
        public void OnCanExecuteChanged()
        {
            InternalCanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
