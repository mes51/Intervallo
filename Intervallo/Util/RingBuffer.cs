using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Util
{
    public class RingBuffer<T> : IList<T>
    {
        public RingBuffer(int capacity)
        {
            Buffer = new T[capacity];
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return Buffer[(RingTailIndex + index) % Buffer.Length];
            }
            set
            {
                if (index < 0 || index >= Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                Buffer[(RingTailIndex + index) % Buffer.Length] = value;
            }
        }

        public int Count
        {
            get
            {
                return Length;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        T[] Buffer { get; }

        int RingTailIndex { get; set; }

        int Length { get; set; }

        EqualityComparer<T> Comparer { get; } = EqualityComparer<T>.Default;

        public void Add(T item)
        {
            var headIndex = (RingTailIndex + Length) % Buffer.Length;
            Buffer[headIndex] = item;
            Length = Math.Min(Length + 1, Buffer.Length);
            if (Length >= Buffer.Length)
            {
                RingTailIndex = (headIndex + 1) % Buffer.Length;
            }
        }

        public void Clear()
        {
            RingTailIndex = 0;
            Length = 0;
        }

        public bool Contains(T item)
        {
            return Buffer.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0, ai = arrayIndex; i < Length && ai < array.Length; i++, ai++)
            {
                array[ai] = this[i];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Length; i++)
            {
                yield return this[i];
            }
        }

        public int IndexOf(T item)
        {
            for (var i = 0; i < Length; i++)
            {
                if (Comparer.Equals(this[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            for (var i = Length - 2; i >= index; i--)
            {
                this[i + 1] = this[i];
            }
            this[index] = item;
            Length = Math.Min(Length + 1, Buffer.Length);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index > -1)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            for (var i = index; i < Length - 1; i++)
            {
                this[i] = this[i + 1];
            }
            Length--;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
