using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins
{
    public static class ExtentionMethods
    {
        public static void Fill<T>(this T[] array, T value)
        {
            array.Fill<T>(value, 0, array.Length);
        }

        public static void Fill<T>(this T[] array, T value, int begin)
        {
            array.Fill<T>(value, begin, array.Length - begin);
        }

        public static void Fill<T>(this T[] array, T value, int begin, int count)
        {
            for (int i = begin, c = 0; c < count; i++, c++)
            {
                array[i] = value;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> body)
        {
            foreach(var e in source)
            {
                body(e);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> body)
        {
            int c = 0;
            foreach (var e in source)
            {
                body(e, c);
                c++;
            }
        }
    }

    public static class PrimitiveArrayExtentionMethods
    {
        public static void BlockCopy(this int[] src, int[] dst)
        {
            src.BlockCopy(0, dst, 0, Math.Min(src.Length, dst.Length));
        }

        public static void BlockCopy(this int[] src, int srcOffset, int[] dst, int dstOffset, int count)
        {
            const int DataSize = sizeof(int);
            Buffer.BlockCopy(src, srcOffset * DataSize, dst, dstOffset * DataSize, count * DataSize);
        }

        public static int[] BlockClone(this int[] src)
        {
            return src.BlockClone(0, src.Length);
        }

        public static int[] BlockClone(this int[] src, int offset, int count)
        {
            var result = new int[count];
            result.BlockCopy(offset, result, 0, count);
            return result;
        }

        public static int[][] BlockClone(this int[][] src)
        {
            return src.BlockClone(0, src.Length);
        }

        public static int[][] BlockClone(this int[][] src, int offset, int count)
        {
            var result = new int[count][];
            for (var i = offset; i < result.Length; i++)
            {
                result[i] = src[i].BlockClone();
            }
            return result;
        }

        public static void BlockCopy(this double[] src, double[] dst)
        {
            src.BlockCopy(0, dst, 0, Math.Min(src.Length, dst.Length));
        }

        public static void BlockCopy(this double[] src, int srcOffset, double[] dst, int dstOffset, int count)
        {
            const int DataSize = sizeof(double);
            Buffer.BlockCopy(src, srcOffset * DataSize, dst, dstOffset * DataSize, count * DataSize);
        }

        public static double[] BlockClone(this double[] src)
        {
            return src.BlockClone(0, src.Length);
        }

        public static double[] BlockClone(this double[] src, int offset, int count)
        {
            var result = new double[count];
            result.BlockCopy(offset, result, 0, count);
            return result;
        }

        public static double[][] BlockClone(this double[][] src)
        {
            return src.BlockClone(0, src.Length);
        }

        public static double[][] BlockClone(this double[][] src, int offset, int count)
        {
            var result = new double[count][];
            for (var i = offset; i < result.Length; i++)
            {
                result[i] = src[i].BlockClone();
            }
            return result;
        }
    }
}
