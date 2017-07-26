using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Model
{
    public class AudioScaleModel
    {
        public AudioScaleModel(double[] f0, double framePeriod, int sampleCount, int sampleRate)
        {
            F0 = f0;
            FramePeriod = framePeriod;
            SampleCount = sampleCount;
            SampleRate = sampleRate;
        }

        public double[] F0 { get; }

        public int FrameLength => F0.Length;

        public double FramePeriod { get; }

        public int SampleCount { get; }

        public int SampleRate { get; }
    }
}
