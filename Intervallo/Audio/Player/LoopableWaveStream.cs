using Intervallo.Plugin;
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

        public LoopableWaveStream(int fs)
        {
            WaveFormat = new WaveFormat(fs, BytePerSample * 8, 2);
            History = new RingBuffer<ReadHistory>(fs);
        }

        public IntRange LoopRange { get; set; }

        public bool EnableLoop { get; set; }

        public int SamplePosition
        {
            get
            {
                return (int)(Position / WaveFormat.BlockAlign);
            }
            set
            {
                Position = value * WaveFormat.BlockAlign;
            }
        }

        public long TotalReadSamples { get; private set; }

        public override long Length
        {
            get
            {
                return (Wave?.Length ?? 0) * WaveFormat.BlockAlign;
            }
        }

        public override long Position { get; set; }

        public override WaveFormat WaveFormat { get; }

        RingBuffer<ReadHistory> History { get; }

        PreviewableStream Wave { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var beginPosition = SamplePosition;
            var beginTotalReadSamples = TotalReadSamples;

            var loopRange = LoopRange ?? new IntRange(0, Wave.SampleCount);
            if (SamplePosition < loopRange.Begin)
            {
                SamplePosition = loopRange.Begin;
            }

            var totalSamples = 0;
            var maxSamples = count / WaveFormat.BlockAlign;
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
                        if (Wave.SamplePosition != SamplePosition)
                        {
                            Wave.SamplePosition = SamplePosition;
                        }
                        for (var c = 0; c < canRead && ms.Position < buffer.Length; c++)
                        {
                            var sampleData = (short)(Wave.ReadSample() * MaxLevel);
                            ms.WriteShort(sampleData);
                            ms.WriteShort(sampleData);
                        }
                        totalSamples += canRead;
                        SamplePosition += canRead;
                    }
                }
            }

            TotalReadSamples += totalSamples;
            History.Add(new ReadHistory(beginTotalReadSamples, beginPosition, TotalReadSamples - beginTotalReadSamples));

            return totalSamples * WaveFormat.BlockAlign;
        }

        public void SetStream(PreviewableStream stream)
        {
            Wave = stream;
            Wave.SamplePosition = SamplePosition;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Wave.Dispose();
        }
    }
}
