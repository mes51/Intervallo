using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Util
{
    public abstract class Optional<T> : IEnumerable<T>
    {
        public abstract T Value { get; }

        public abstract bool IsEmpty { get; }

        public bool IsDefined
        {
            get
            {
                return !IsEmpty;
            }
        }

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Optional<T> FromNull(T value)
        {
            if (Equals(value, null))
            {
                return new None<T>();
            }
            else
            {
                return new Some<T>(value);
            }
        }

        public static Optional<T> None()
        {
            return new None<T>();
        }

        public static Optional<T> Some(T value)
        {
            return new Some<T>(value);
        }
    }

    public class Some<T> : Optional<T>
    {
        internal Some(T value)
        {
            Value = value;
        }

        public override T Value { get; }

        public override bool IsEmpty
        {
            get
            {
                return false;
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            yield return Value;
        }
    }

    public class None<T> : Optional<T>
    {
        internal None() { }

        public override T Value
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override bool IsEmpty
        {
            get
            {
                return true;
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }
    }
}
