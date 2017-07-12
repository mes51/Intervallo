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
    public class Range : IEnumerable<int>, IEquatable<Range>
    {
        public Range() : this(0, 0) { }

        public Range(int begin, int end)
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

        public Range Move(int pos)
        {
            return new Range(Begin + pos, End + pos);
        }

        public Range MoveTo(int begin)
        {
            return new Range(begin, begin + Length);
        }

        public Range Adjust(Range bounds)
        {
            var begin = Math.Max(Math.Min(End, bounds.End) - Length, bounds.Begin);
            return new Range(begin, begin + Math.Min(Length, bounds.Length));
        }

        public Range Stretch(int v)
        {
            return new Range(Begin, Math.Max(End + v, Begin));
        }

        public Range Intersect(Range range)
        {
            return new Range(
                Math.Max(Math.Min(Begin, range.End), range.Begin),
                Math.Max(Math.Min(End, range.End), range.Begin)
            );
        }

        public Range Union(Range range)
        {
            return new Range(
                Math.Min(Begin, range.Begin),
                Math.Max(End, range.End)
            );
        }

        public bool IsInclude(int n)
        {
            return n >= Begin && n < End;
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

        public bool Equals(Range other)
        {
            return Begin == other.Begin && End == other.End;
        }

        public override bool Equals(object obj)
        {
            return obj is Range && Equals((Range)obj);
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
        public static Range To(this int begin, int end)
        {
            return new Range(begin, end);
        }

        public static Range From(this int end, int begin)
        {
            return new Range(begin, end);
        }
    }
}
