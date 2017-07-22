using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.WORLD
{
    class SubSequence<T> : IList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        public T[] Array { get; private set; }

        public int Offset { get; private set; }

        public int Length { get; private set; }

        public SubSequence(T[] array, int offset, int length)
        {
            Array = array;
            Offset = offset;
            Length = length;
        }

        public SubSequence(SubSequence<T> a, int offset, int length)
        {
            Array = a.Array;
            Offset = a.Offset + offset;
            Length = length;
        }

        public SubSequence(T[] array, int offset)
            : this(array, offset, Math.Max(array.Length - offset, 0)) { }

        public SubSequence(SubSequence<T> array, int offset)
            : this(array, offset, Math.Max(array.Length - offset, 0)) { }

        public SubSequence(T[] array)
            : this(array, 0, array.Length) { }

        public SubSequence(SubSequence<T> array)
            : this(array, 0, array.Length) { }

        public int Count => Length;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                return Array[Offset + index];
            }
            set
            {
                Array[Offset + index] = value;
            }
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            System.Array.Clear(Array, Offset, Length);
        }

        public bool Contains(T item)
        {
            return Array.Skip(Offset).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            System.Array.Copy(Array, Offset, array, arrayIndex, Math.Min(Length, array.Length - arrayIndex));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Array).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return System.Array.IndexOf<T>(Array, item, Offset);
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Array.GetEnumerator();
        }

        public static implicit operator SubSequence<T>(T[] array)
        {
            return new SubSequence<T>(array);
        }
    }

    static class SubSequenceExtension
    {
        public static SubSequence<T> SubSequence<T>(this T[] array, int offset, int length)
        {
            return new SubSequence<T>(array, offset, length);
        }

        public static SubSequence<T> SubSequence<T>(this T[] array, int offset)
        {
            return new SubSequence<T>(array, offset);
        }

        public static SubSequence<T> SubSequence<T>(this SubSequence<T> array, int offset, int length)
        {
            return new SubSequence<T>(array, offset, length);
        }

        public static SubSequence<T> SubSequence<T>(this SubSequence<T> array, int offset)
        {
            return new SubSequence<T>(array, offset);
        }
    }
}
