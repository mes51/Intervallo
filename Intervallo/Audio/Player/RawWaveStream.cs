using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    public class RawWaveStream : PreviewableStream
    {
        public RawWaveStream(double[] wave)
        {
            Wave = wave;
        }

        public override long Length => Wave.Length * sizeof(double);

        double[] Wave { get; }

        public override int ReadSamples(double[] buffer, int count)
        {
            if (count < 1)
            {
                return 0;
            }

            var copyCount = Math.Min(SampleCount - SamplePosition, count);
            Buffer.BlockCopy(Wave, SamplePosition * sizeof(double), buffer, 0, copyCount * sizeof(double));

            SamplePosition += copyCount;

            return copyCount;
        }
    }
}
