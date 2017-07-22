using Intervallo.Util;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    class LoopableWaveStream : WaveStream
    {
        internal class ReadHistory
        {
            public ReadHistory(long beginTotalReadSamples, long beginReadPosition, long readSamples)
            {
                BeginTotalReadSamples = beginTotalReadSamples;
                BeginReadPosition = beginReadPosition;
                ReadSamples = readSamples;
            }

            public long BeginTotalReadSamples { get; }

            public long BeginReadPosition { get; }

            public long ReadSamples { get; }
        }

        const int BytePerSample = 2;
        const int MaxLevel = 1 << (BytePerSample * 8 - 1);

        public LoopableWaveStream(double[] wave, int fs)
        {
            Wave = wave;
            WaveFormat = new WaveFormat(fs, BytePerSample * 8, 1);
            History = new RingBuffer<ReadHistory>(fs);
        }

        public double[] Wave { get; }

        public IntRange LoopRange { get; set; }

        public bool EnableLoop { get; set; }

        public int SamplePosition
        {
            get
            {
                return (int)(Position / BytePerSample);
            }
            set
            {
                Position = value * BytePerSample;
            }
        }

        public long TotalReadSamples { get; private set; }

        public override long Length
        {
            get
            {
                return Wave.Length * BytePerSample;
            }
        }

        public override long Position { get; set; }

        public override WaveFormat WaveFormat { get; }

        RingBuffer<ReadHistory> History { get; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var beginPosition = SamplePosition;
            var beginTotalReadSamples = TotalReadSamples;

            var loopRange = LoopRange ?? new IntRange(0, Wave.Length);
            if (SamplePosition < loopRange.Begin * BytePerSample)
            {
                SamplePosition = loopRange.Begin * BytePerSample;
            }

            var totalSamples = 0;
            var maxSamples = count / BytePerSample;
            using (var ms = new MemoryStream(buffer))
            {
                ms.Seek(offset, SeekOrigin.Begin);
                while (totalSamples < maxSamples)
                {
                    var canRead = (int)Math.Min(maxSamples - totalSamples, loopRange.End - SamplePosition);
                    if (canRead <= 0)
                    {
                        if (EnableLoop)
                        {
                            History.Add(new ReadHistory(beginPosition, beginTotalReadSamples, totalSamples - (beginTotalReadSamples - TotalReadSamples)));
                            SamplePosition = loopRange.Begin;
                            beginPosition = loopRange.Begin;
                            beginTotalReadSamples = TotalReadSamples + totalSamples;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        for (int s = SamplePosition, c = 0; c < canRead && ms.Position < buffer.Length; s++, c++)
                        {
                            var sampleData = (int)(Wave[s] * MaxLevel);
                            for (var d = 0; d < BytePerSample; d++, sampleData >>= 8)
                            {
                                ms.WriteByte((byte)(sampleData & 0xff));
                            }
                        }
                        totalSamples += canRead;
                        SamplePosition += canRead;
                    }
                }
            }

            TotalReadSamples += totalSamples;
            History.Add(new ReadHistory(beginTotalReadSamples, beginPosition, TotalReadSamples - beginTotalReadSamples));

            return totalSamples * BytePerSample;
        }

        internal ReadHistory GetHistory(long totalReadCount)
        {
            for (var i = History.Count - 1; i > -1; i--)
            {
                var h = History[i];
                if (h.BeginTotalReadSamples > totalReadCount && h.BeginTotalReadSamples + h.ReadSamples > totalReadCount)
                {
                    return h;
                }
            }

            return null;
        }
    }
}
