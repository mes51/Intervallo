using Intervallo.InternalUtil;
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

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T value)
        {
            return source.Concat(new T[] { value });
        }

        public static IEnumerable<TResult> SplitByIndexes<TSource, TResult>(this IEnumerable<TSource> source, IEnumerable<int> indexes, Func<IEnumerable<TSource>, int, TResult> selector)
        {
            return source.SplitByIndexes(indexes, (e, i, _) => selector(e, i));
        }

        public static IEnumerable<TResult> SplitByIndexes<TSource, TResult>(this IEnumerable<TSource> source, IEnumerable<int> indexes, Func<IEnumerable<TSource>, int, int, TResult> selector)
        {
            var traversal = source;
            var prevIndex = indexes.First();
            var i = 0;
            foreach (var index in indexes.Skip(1))
            {
                yield return selector(traversal.Take(index - prevIndex), prevIndex, i);
                traversal = traversal.Skip(index - prevIndex);
                prevIndex = index;
                i++;
            }
            yield return selector(traversal, prevIndex, i);
        }

        public static IEnumerable<TResult> SplitByIndexes<TSource, TResult>(this TSource[] source, IEnumerable<int> indexes, Func<IEnumerable<TSource>, int, TResult> selector)
        {
            return source.SplitByIndexes(indexes, (e, i, _) => selector(e, i));
        }

        public static IEnumerable<TResult> SplitByIndexes<TSource, TResult>(this TSource[] source, IEnumerable<int> indexes, Func<IEnumerable<TSource>, int, int, TResult> selector)
        {
            var prevIndex = indexes.First();
            var i = 0;
            foreach (var index in indexes.Skip(1))
            {
                var e = new TSource[index - prevIndex];
                Array.Copy(source, prevIndex, e, 0, e.Length);
                yield return selector(e, prevIndex, i);
                prevIndex = index;
                i++;
            }

            var eLast = new TSource[source.Length - prevIndex];
            Array.Copy(source, prevIndex, eLast, 0, eLast.Length);
            yield return selector(eLast, prevIndex, i);
        }

        public static IEnumerable<TResult> Zip3<TFirst, TSecond, TThird, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, Func<TFirst, TSecond, TThird, TResult> resultSelector)
        {
            using (var f = first.GetEnumerator())
            using (var s = second.GetEnumerator())
            using (var t = third.GetEnumerator())
            {
                while (f.MoveNext() && s.MoveNext() && t.MoveNext())
                {
                    yield return resultSelector(f.Current, s.Current, t.Current);
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> Grouped<T>(this IEnumerable<T> source, int count)
        {
            var traversal = source;
            while (traversal.Any())
            {
                yield return traversal.Take(count);
                traversal = traversal.Skip(count);
            }
        }
    }

    public static class PrimitiveArrayExtentionMethods
    {
        public static void BlockCopy(this int[] src, int[] dst)
        {
            src.BlockCopy(0, dst, 0, Math.Min(src.Length, dst.Length));
        }

        public static void BlockCopy(this int[] src, int[] dst, int dstOffset)
        {
            src.BlockCopy(0, dst, dstOffset, Math.Min(src.Length, dst.Length - dstOffset));
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
            src.BlockCopy(offset, result, 0, count);
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

        public static void BlockCopy(this double[] src, double[] dst, int dstOffset)
        {
            src.BlockCopy(0, dst, dstOffset, Math.Min(src.Length, dst.Length - dstOffset));
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
            src.BlockCopy(offset, result, 0, count);
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
