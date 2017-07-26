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
        public AnalyzedAudioCache(Type operatorType, AnalyzedAudio analyzedAudio, int sampleCount, int sampleRate, string hash)
        {
            OperatorType = operatorType;
            AnalyzedAudio = analyzedAudio;
            SampleCount = sampleCount;
            SampleRate = sampleRate;
            AudioHash = hash;
        }

        public Type OperatorType { get; }

        public AnalyzedAudio AnalyzedAudio { get; }

        public int SampleCount { get; }

        public int SampleRate { get; }

        public string AudioHash { get; }
    }
}
