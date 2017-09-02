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
