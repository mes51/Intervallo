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
    [Export(typeof(IAudioOperator))]
    public class WorldOperator : IAudioOperator
    {
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResource.WorldOperatorDescription;

        public string PluginName => typeof(WorldOperator).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public Task<AnalyzedAudio> Analyze(WaveData wave, double framePeriod, Action<double> notifyProgress)
        {
            throw new NotImplementedException();
        }

        public Task<WaveData> Synthesize(AnalyzedAudio analyzedAudio, Action<double> notifyProgress)
        {
            throw new NotImplementedException();
        }
    }
}
