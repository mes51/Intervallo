using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intervallo.Form;

namespace Intervallo.Command
{
    public class PreviewCommand : CommandBase
    {
        public PreviewCommand(MainWindow window) : base(window) { }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) && Window.MainView.Wave != null;
        }

        public override void Execute(object parameter)
        {
            Window.ExecPlayOrStop();
        }
    }
}
