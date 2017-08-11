using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intervallo.Form;

namespace Intervallo.Command
{
    public class OpenCommand : CommandBase
    {
        public OpenCommand(MainWindow window) : base(window) { }

        public override void Execute(object parameter)
        {
            Window.ExecOpen();
        }
    }
}
