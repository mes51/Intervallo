using Intervallo.Form;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Intervallo.Command
{
    public class ExportCommand : CommandBase
    {
        public ExportCommand(MainWindow window) : base(window) { }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) && Window.MainView.Wave != null;
        }

        public override void Execute(object parameter)
        {
            Window.Dispatcher.Invoke(() => Window.ExecExportWave());
        }
    }
}
