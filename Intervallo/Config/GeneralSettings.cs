using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Intervallo.Config
{
    [DataContract]
    public class GeneralSettings
    {
        [DataMember(Name = "position")]
        public Point Position { get; set; } = new Point(300.0, 300.0);

        [DataMember(Name = "size")]
        public Size Size { get; set; } = new Size(945.0, 610.0);

        [DataMember(Name = "state")]
        public WindowState State { get; set; } = WindowState.Normal;

        [DataMember(Name = "showExceptionInMessageBox")]
        public bool ShowExceptionInMessageBox { get; set; } = false;
    }
}
