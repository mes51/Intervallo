using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Intervallo.Config
{
    [DataContract]
    public class ApplicationSettings
    {
        static readonly string FilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");

        public static ApplicationSettings Setting { get; }

        static ApplicationSettings()
        {
            Setting = Load();
        }

        public ApplicationSettings() { }

        [DataMember(Name = "position")]
        public Point Position { get; set; } = new Point(300.0, 300.0);

        [DataMember(Name = "size")]
        public Size Size { get; set; } = new Size(945.0, 610.0);

        [DataMember(Name = "state")]
        public WindowState State { get; set; } = WindowState.Normal;

        public void Save()
        {
            try
            {
                using (var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ApplicationSettings));
                    serializer.WriteObject(fs, this);
                }
            }
            catch { }
        }

        static ApplicationSettings Load()
        {
            try
            {
                using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ApplicationSettings));
                    return (serializer.ReadObject(fs) as ApplicationSettings) ?? new ApplicationSettings();
                }
            }
            catch
            {
                return new ApplicationSettings();
            }
        }
    }
}
