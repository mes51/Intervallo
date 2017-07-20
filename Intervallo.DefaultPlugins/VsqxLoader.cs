using Intervallo.DefaultPlugins.Properties;
using Intervallo.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins
{
    [Export(typeof(IScaleLoader))]
    public class VsqxLoader : IScaleLoader
    {
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResource.VsqxLoaderDescription;

        public string PluginName => typeof(WorldOperator).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public double[] Load(string filePath, double framePeriod)
        {
            throw new NotImplementedException();
        }
    }
}
