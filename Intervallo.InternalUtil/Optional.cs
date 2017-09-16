using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intervallo.InternalUtil
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

        public T GetOrElse(T defaultValue)
        {
            if (IsDefined)
            {
                return Value;
            }
            else
            {
                return defaultValue;
            }
        }

        public T GetOrElse(Func<T> defaultValue)
        {
            if (IsDefined)
            {
                return Value;
            }
            else
            {
                return defaultValue();
            }
        }

        public Optional<T> OrElse(Optional<T> defaultValue)
        {
            if (IsDefined)
            {
                return this;
            }
            else
            {
                return defaultValue;
            }
        }

        public Optional<T> OrElse(Func<Optional<T>> defaultValue)
        {
            if (IsDefined)
            {
                return this;
            }
            else
            {
                return defaultValue();
            }
        }

        public Optional<TResult> Select<TResult>(Func<T, TResult> func)
        {
            if (IsDefined)
            {
                return new Some<TResult>(func(Value));
            }
            else
            {
                return new None<TResult>();
            }
        }

        public Optional<TResult> SelectMany<TResult>(Func<T, Optional<TResult>> func)
        {
            if (IsDefined)
            {
                return func(Value);
            }
            else
            {
                return new None<TResult>();
            }
        }

        public Optional<T> Where(Predicate<T> predicate)
        {
            if (IsDefined && predicate(Value))
            {
                return this;
            }
            else
            {
                return new None<T>();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (IsDefined)
            {
                yield return Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Optional<T> FromNull(T value)
        {
            if (Equals(value, null))
            {
                return None();
            }
            else
            {
                return Some(value);
            }
        }

        public static Optional<T> Iif(T value, bool a)
        {
            if (a)
            {
                return Some(value);
            }
            else
            {
                return None();
            }
        }

        public static Optional<T> Iif(Func<T> func, bool a)
        {
            if (a)
            {
                return Some(func());
            }
            else
            {
                return None();
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
    }

    public class None<T> : Optional<T>
    {
        internal None() { }

        public override T Value
        {
            get
            {
                throw new NoElementException();
            }
        }

        public override bool IsEmpty
        {
            get
            {
                return true;
            }
        }
    }
}
