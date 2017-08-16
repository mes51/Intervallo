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
        public ActionCommand(MainWindow window, Action action) : base(window)
        {
            Action = action;
        }

        Action Action { get; }

        public override void Execute(object parameter)
        {
            Action();
        }
    }
}
