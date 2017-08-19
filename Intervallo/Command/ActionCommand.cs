using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intervallo.Form;

namespace Intervallo.Command
{
    public class ActionCommand : CommandBase
    {
        public ActionCommand(MainWindow window, Action action) : this(window, action, false) { }

        public ActionCommand(MainWindow window, Action action, bool forceEnable) : base(window)
        {
            Action = action;
            ForceEnable = forceEnable;
        }

        Action Action { get; }

        bool ForceEnable { get; }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) || ForceEnable;
        }

        public override void Execute(object parameter)
        {
            Action();
        }
    }
}
