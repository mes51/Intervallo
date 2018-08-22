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
