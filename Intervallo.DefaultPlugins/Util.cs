using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins
{
    static class MathUtil
    {
        public static double Log2(double d)
        {
            const double Log2 = 0.693147180559945; //Math.Log(2);
            return Math.Log(d) / Log2;
        }
    }

    static class Util
    {
        public static T[][] Mak2DArray<T>(int row, int col)
        {
            return Enumerable.Range(0, row).Select((x) => new T[col]).ToArray();
        }

        public static void FillEmptyFrame(double[] f0)
        {
            var emptyStarted = -1;
            for (var i = 0; i < f0.Length; i++)
            {
                if (f0[i] <= 0.0)
                {
                    if (emptyStarted < 0)
                    {
                        emptyStarted = i;
                    }
                }
                else if (emptyStarted > -1)
                {
                    var prevEndFrame = emptyStarted != 0 ? f0[emptyStarted - 1] : 0.0;
                    var nextBeginFrame = f0[i];

                    if (prevEndFrame <= 0.0)
                    {
                        f0.Fill(nextBeginFrame, 0, i);
                    }
                    else
                    {
                        var center = (i - emptyStarted) / 2;
                        f0.Fill(prevEndFrame, emptyStarted, center);
                        f0.Fill(nextBeginFrame, center + emptyStarted, (i - emptyStarted) - center);
                    }

                    emptyStarted = -1;
                }
            }

            if (emptyStarted > -1)
            {
                f0.Fill(f0[emptyStarted - 1], emptyStarted);
            }
        }
    }

    static class Interpolation
    {
        public static double CatmullRom(double value0, double value1, double value2, double value3, double time1, double time2, double time)
        {
            double t = 1.0 / (time2 - time1) * (time - time1);
            double v0 = (value2 - value0) / 2.0;
            double v1 = (value3 - value1) / 2.0;
            double t2 = t * t;
            double t3 = t2 * t;
            return (2 * value1 - 2 * value2 + v0 + v1) * t3 + (-3 * value1 + 3 * value2 - 2 * v0 - v1) * t2 + v0 * t + value1;
        }

        public static double Linear(double value1, double value2, double time1, double time2, double time)
        {
            var t = (time - time1) / (time2 - time1);
            return (value2 - value1) * t + value1;
        }
    }

    static class Frequency
    {
        public static double FromNoteNumber(double noteNumber)
        {
            return 440.0 * Math.Pow(2, (noteNumber - 69) / 12.0);
        }
    }

    static class EnumerableUtil
    {
        public static IEnumerable<T> Infinity<T>(T value)
        {
            while (true)
            {
                yield return value;
            }
        }
    }
}
