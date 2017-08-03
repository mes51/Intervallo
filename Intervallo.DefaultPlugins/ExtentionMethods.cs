using Intervallo.Plugin.Util;
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

        public static RangeDictionary<TKey, TSource> ToRangeDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IntervalMode intervalMode) where TKey : IComparable<TKey>
        {
            return source.ToRangeDictionary(keySelector, (v) => v, intervalMode);
        }

        public static RangeDictionary<TKey, TValue> ToRangeDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IntervalMode intervalMode) where TKey : IComparable<TKey>
        {
            var result = new RangeDictionary<TKey, TValue>(intervalMode);
            foreach (var v in source)
            {
                var key = keySelector(v);
                if (!result.ContainsKey(key))
                {
                    result.Add(keySelector(v), valueSelector(v));
                }
                else
                {
                    result[key] = valueSelector(v);
                }
            }
            return result;
        }

        public static IEnumerable<TResult> SelectReferencePrev<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Optional<TResult>, TResult> selector)
        {
            return source.SelectReferencePrev<TSource, TResult>((v, i) => selector(v, Optional<TResult>.None()), (v, i, p) => selector(v, Optional<TResult>.Some(p)));
        }

        public static IEnumerable<TResult> SelectReferencePrev<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, Optional<TResult>, TResult> selector)
        {
            return source.SelectReferencePrev<TSource, TResult>((v, i) => selector(v, i, Optional<TResult>.None()), (v, i, p) => selector(v, i, Optional<TResult>.Some(p)));
        }

        public static IEnumerable<TResult> SelectReferencePrev<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> firstSelector, Func<TSource, TResult, TResult> selector)
        {
            return source.SelectReferencePrev((v, i) => firstSelector(v), (v, i, p) => selector(v, p));
        }

        public static IEnumerable<TResult> SelectReferencePrev<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> firstSelector, Func<TSource, int, TResult, TResult> selector)
        {
            var prev = default(TResult);
            foreach (var v in source.Take(1))
            {
                prev = firstSelector(v, 0);
                yield return prev;
            }
            var i = 1;
            foreach (var v in source.Skip(1))
            {
                prev = selector(v, i, prev);
                i++;
                yield return prev;
            }
        }

        public static IEnumerable<T> PushTo<T>(this T value, IEnumerable<T> target)
        {
            return new T[] { value }.Concat(target);
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
