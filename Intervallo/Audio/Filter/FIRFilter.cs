using System;
using System.Linq;

namespace Intervallo.Audio.Filter
{
    // see: http://aidiary.hatenablog.com/entry/20111028/1319815300
    public static class FIRFilter
    {
        public static double[] Process(double[] sample, double[] b)
        {
            var result = new double[sample.Length];
            var N = b.Length - 1;

            for (var i = 0; i < sample.Length; i++)
            {
                for (int n = i, c = 0; n > -1 && c < b.Length; n--, c++)
                {
                    result[n] += b[c] * sample[n];
                }
            }

            return result;
        }

        public static double[] CreateLPFCoefficient(double fs, double edgeFs, int tap)
        {
            var center = edgeFs / (fs * 0.5);
            var omega = Math.PI * center;
            var halfLength = (tap - 1) / 2 + 1;
            var half = CreateHanningWindow(halfLength)
                .Select((w, i) =>
                {
                    var coeff = 1.0 / (Math.PI * i) * Math.Sin(i * omega);
                    if (double.IsInfinity(coeff) || double.IsNaN(coeff))
                    {
                        coeff = 1.0;
                    }
                    return w * coeff;
                })
                .Skip(1)
                .ToArray();
            return half.Reverse().Concat(new double[] { center }).Concat(half).ToArray();
        }

        static double[] CreateHanningWindow(int length)
        {
            var x = 1.0 / (length - 1);

            return Enumerable.Range(0, length)
                .Select((i) => 0.5 + 0.5 * Math.Cos(Math.PI * x * i))
                .ToArray();
        }
    }
}
