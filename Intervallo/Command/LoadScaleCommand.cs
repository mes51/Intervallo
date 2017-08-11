using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intervallo.Form;
using Intervallo.Plugin;

namespace Intervallo.Command
{
    public class LoadScaleCommand : CommandBase
    {
        public LoadScaleCommand(MainWindow window, bool usePlugin) : base(window)
        {
            UsePlugin = usePlugin;
        }

        public bool UsePlugin { get; }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) && Window.MainView.Wave != null && (!UsePlugin || parameter is IScaleLoader);
        }

        public override void Execute(object parameter)
        {
            if (UsePlugin)
            {
                Window.ExecLoadScale(parameter as IScaleLoader);
            }
            else
            {
                Window.ExecLoadScaleFromWave();
            }
        }
    }
}
