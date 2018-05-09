using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Util
{
    /// <summary>
    /// Exclude End
    /// </summary>
    public class IntRange : IEnumerable<int>, IEquatable<IntRange>
    {
        public IntRange() : this(0, 0) { }

        public IntRange(int begin, int end)
        {
            if (begin > end)
            {
                throw new ArgumentException(nameof(begin));
            }

            Begin = begin;
            End = end;
        }

        public int Begin { get; }

        public int End { get; }

        public int Length => End - Begin;

        public IntRange Move(int pos)
        {
            return new IntRange(Begin + pos, End + pos);
        }

        public IntRange MoveTo(int begin)
        {
            return new IntRange(begin, begin + Length);
        }

        public IntRange Adjust(IntRange bounds)
        {
            var begin = Math.Max(Math.Min(End, bounds.End) - Length, bounds.Begin);
            return new IntRange(begin, begin + Math.Min(Length, bounds.Length));
        }

        public IntRange Stretch(int v)
        {
            return new IntRange(Begin, Math.Max(End + v, Begin));
        }

        public IntRange Intersect(IntRange range)
        {
            return new IntRange(
                Math.Max(Math.Min(Begin, range.End), range.Begin),
                Math.Max(Math.Min(End, range.End), range.Begin)
            );
        }

        public IntRange Union(IntRange range)
        {
            return new IntRange(
                Math.Min(Begin, range.Begin),
                Math.Max(End, range.End)
            );
        }

        public bool IsInclude(int n)
        {
            return n >= Begin && n < End;
        }

        public bool IsInclude(IntRange range)
        {
            return range.Begin >= Begin && range.End <= End;
        }

        public bool IsOverlap(IntRange range)
        {
            return range.End >= Begin || range.Begin <= End;
        }

        public int ClipValue(int v)
        {
            return Math.Max(Math.Min(v, End), Begin);
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (var i = Begin; i < End; i++)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(IntRange other)
        {
            return Begin == other.Begin && End == other.End;
        }

        public override bool Equals(object obj)
        {
            return obj is IntRange && Equals((IntRange)obj);
        }

        public override string ToString()
        {
            return $"{Begin}...{End}";
        }

        public override int GetHashCode()
        {
            return Begin.GetHashCode() ^ End.GetHashCode();
        }
    }

    /// <summary>
    /// Exclude End
    /// </summary>
    public class DoubleRange : IEnumerable<double>, IEquatable<DoubleRange>
    {
        public DoubleRange() : this(0.0, 0.0, 1.0) { }

        public DoubleRange(double begin, double end) : this(begin, end, 1.0) { }

        public DoubleRange(double begin, double end, double enumStep)
        {
            if (begin > end)
            {
                throw new ArgumentException(nameof(begin));
            }

            Begin = begin;
            End = end;
            EnumStep = enumStep;
        }

        public double Begin { get; }

        public double End { get; }

        public double EnumStep { get; }

        public double Length => End - Begin;

        public DoubleRange Move(double pos)
        {
            return new DoubleRange(Begin + pos, End + pos);
        }

        public DoubleRange MoveTo(double begin)
        {
            return new DoubleRange(begin, begin + Length);
        }

        public DoubleRange Adjust(DoubleRange bounds)
        {
            var begin = Math.Max(Math.Min(End, bounds.End) - Length, bounds.Begin);
            return new DoubleRange(begin, begin + Math.Min(Length, bounds.Length));
        }

        public DoubleRange Stretch(double v)
        {
            return new DoubleRange(Begin, Math.Max(End + v, Begin));
        }

        public DoubleRange Intersect(DoubleRange range)
        {
            return new DoubleRange(
                Math.Max(Math.Min(Begin, range.End), range.Begin),
                Math.Max(Math.Min(End, range.End), range.Begin)
            );
        }

        public DoubleRange Union(DoubleRange range)
        {
            return new DoubleRange(
                Math.Min(Begin, range.Begin),
                Math.Max(End, range.End)
            );
        }

        public bool IsInclude(double n)
        {
            return n >= Begin && n < End;
        }

        public bool IsOverlap(IntRange range)
        {
            return range.End >= Begin || range.Begin <= End;
        }

        public double ClipValue(double v)
        {
            return Math.Max(Math.Min(v, End), Begin);
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (var i = Begin; i < End; i += EnumStep)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(DoubleRange other)
        {
            return Begin == other.Begin && End == other.End;
        }

        public override bool Equals(object obj)
        {
            return obj is DoubleRange && Equals((DoubleRange)obj);
        }

        public override string ToString()
        {
            return $"{Begin}...{End}";
        }

        public override int GetHashCode()
        {
            return Begin.GetHashCode() ^ End.GetHashCode();
        }
    }

    public static class RangeExtention
    {
        public static IntRange To(this int begin, int end)
        {
            return new IntRange(begin, end);
        }

        public static IntRange From(this int end, int begin)
        {
            return new IntRange(begin, end);
        }

        public static DoubleRange To(this double begin, double end)
        {
            return new DoubleRange(begin, end);
        }

        public static DoubleRange To(this double begin, double end, double enumStep)
        {
            return new DoubleRange(begin, end, enumStep);
        }

        public static DoubleRange From(this double end, double begin)
        {
            return new DoubleRange(begin, end);
        }

        public static DoubleRange From(this double end, double begin, double enumStep)
        {
            return new DoubleRange(begin, end, enumStep);
        }
    }
}
