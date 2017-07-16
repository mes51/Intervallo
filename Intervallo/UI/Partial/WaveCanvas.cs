using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.UI
{
    partial class WaveCanvas
    {
        enum WaveLineType
        {
            PolyLine,
            Bar
        }

        class WaveLine
        {

            public WaveLine(float[][] line, WaveLineType type)
            {
                Line = line;
                Type = type;
            }

            public float[][] Line { get; }

            public WaveLineType Type { get; }
        }

        class WaveLineMap
        {
            public const double DefaultPathHeight = 1000.0;

            readonly int[] ReductionCounts = new int[] { 200, 2000 };

            public WaveLineMap(double[] wave)
            {
                var center = DefaultPathHeight * 0.5;
                WaveLines.Add(0.0, new WaveLine(wave.Select((w) => new float[] { (float)(w * center + center) }).ToArray(), WaveLineType.PolyLine));
                foreach (var r in ReductionCounts)
                {
                    WaveLines.Add(r, new WaveLine(CreateReductedWaveLine(wave, r), WaveLineType.Bar));
                }
            }

            public RangeDictionary<double, WaveLine> WaveLines { get; } = new RangeDictionary<double, WaveLine>(IntervalMode.OpenInterval);

            static float[][] CreateReductedWaveLine(double[] wave, int reductionCount)
            {
                var center = DefaultPathHeight * 0.5;

                var points = new float[(int)Math.Ceiling(wave.Length / (double)reductionCount)][];
                for (int i = 1, v = 0; i < wave.Length; v++)
                {
                    var max = wave[i - 1];
                    var min = wave[i - 1];
                    for (var c = i % reductionCount; c < reductionCount && i < wave.Length; c++, i++)
                    {
                        max = Math.Max(max, wave[i]);
                        min = Math.Min(min, wave[i]);
                    }
                    points[v] = new float[] { (float)(min * center + center), (float)(max * center + center) };
                }

                return points;
            }
        }
    }
}
