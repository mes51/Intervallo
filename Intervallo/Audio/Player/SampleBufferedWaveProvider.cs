using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    class SampleBufferedWaveProvider : IWaveProvider
    {
        const int BytePerSample = 2;
        const int MaxLevel = 1 << (BytePerSample * 8 - 1);
        const int DoubleSize = sizeof(double);

        public SampleBufferedWaveProvider(int fs)
        {
            WaveFormat = new WaveFormat(fs, BytePerSample * 8, 1);
        }

        public WaveFormat WaveFormat { get; }

        LinkedList<double[]> SampleQueue { get; } = new LinkedList<double[]>();

        double[] RemainSample { get; set; } = new double[0];

        public void AddSamples(double[] samples)
        {
            var windowedSample = new double[samples.Length];
            for (var i = 0; i < samples.Length; i++)
            {
                windowedSample[i] = samples[i] * GetSampleWindow(samples.Length, i);
            }
            lock(SampleQueue)
            {
                SampleQueue.AddLast(windowedSample);
            }
        }

        public void ClearBuffer()
        {
            lock (SampleQueue)
            {
                SampleQueue.Clear();
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count < 1)
            {
                return 0;
            }

            var writeCount = 0;
            var lastWriteSampleCount = 0;
            var lastSamples = new double[0];
            using (var ms = new MemoryStream(buffer))
            {
                if (RemainSample.Length > 0)
                {
                    lastWriteSampleCount = WriteSample(ms, RemainSample);
                    writeCount += lastWriteSampleCount * BytePerSample;
                    lastSamples = RemainSample;
                }
                else
                {
                    while (writeCount < count)
                    {
                        lock(SampleQueue)
                        {
                            if (SampleQueue.Count < 1)
                            {
                                break;
                            }

                            var samples = SampleQueue.First();
                            SampleQueue.RemoveFirst();

                            lastWriteSampleCount = WriteSample(ms, samples);
                            writeCount += lastWriteSampleCount * BytePerSample;
                            lastSamples = samples;
                        }
                    }
                }
            }

            if (writeCount >= count && lastWriteSampleCount < lastSamples.Length - 1)
            {
                RemainSample = new double[lastSamples.Length - lastWriteSampleCount - 1];
                Buffer.BlockCopy(lastSamples, (lastWriteSampleCount + 1) * DoubleSize, RemainSample, 0, RemainSample.Length * DoubleSize);
            }
            else
            {
                RemainSample = new double[0];
            }

            if (writeCount < count)
            {
                Array.Clear(buffer, offset + writeCount, count - writeCount);
            }

            return count;
        }

        int WriteSample(MemoryStream ms, double[] samples)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var sampleData = (int)(samples[i] * MaxLevel);
                for (var d = 0; d < BytePerSample; d++, sampleData >>= 8)
                {
                    ms.WriteByte((byte)(sampleData & 0xff));
                }

                if (ms.Position >= ms.Length)
                {
                    return i + 1;
                }
            }

            return samples.Length;
        }

        double GetSampleWindow(int sampleCount, int index)
        {
            var edgeArea = (int)(sampleCount * 0.05);
            var p = Math.PI / edgeArea;
            if (index < edgeArea)
            {
                return Math.Cos(p * index - Math.PI) * 0.5 + 0.5;
            }
            else if (index + edgeArea >= sampleCount)
            {
                return Math.Cos(Math.PI - p * (sampleCount - index)) * 0.5 + 0.5;
            }
            else
            {
                return 1.0;
            }
        }
    }
}
