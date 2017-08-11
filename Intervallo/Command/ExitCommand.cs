using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intervallo.Form;

namespace Intervallo.Command
{
    public class ExitCommand : CommandBase
    {
        public ExitCommand(MainWindow window) : base(window) { }

        public override bool CanExecute(object parameter)
        {
            return true;
        }

        public override void Execute(object parameter)
        {
            Window.Close();
        }
    }
}
