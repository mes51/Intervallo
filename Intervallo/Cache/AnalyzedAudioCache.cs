using Intervallo.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Cache
{
    [Serializable]
    public class AnalyzedAudioCache
    {
        public AnalyzedAudioCache(Type operatorType, AnalyzedAudio analyzedAudio, string hash)
        {
            OperatorType = operatorType;
            AnalyzedAudio = analyzedAudio;
            AudioHash = hash;
        }

        public Type OperatorType { get; }

        public AnalyzedAudio AnalyzedAudio { get; }

        public string AudioHash { get; }
    }
}
