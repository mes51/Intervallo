using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intervallo.Form;

namespace Intervallo.Command
{
    public class WaveCommand : CommandBase
    {
        public WaveCommand(MainWindow window, Action action) : this(window, _ => action()) { }

        public WaveCommand(MainWindow window, Action<object> action) : base(window)
        {
            Action = action;
        }

        Action<object> Action { get; }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) && Window.MainView.Wave != null;
        }

        public override void Execute(object parameter)
        {
            Action(parameter);
        }
    }
}
