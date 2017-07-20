using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Util
{
    public abstract class Try<T> : IEnumerable<T>
    {
        public abstract T Value { get; }

        public abstract bool IsFailure { get; }

        public bool IsSuccess
        {
            get
            {
                return !IsFailure;
            }
        }

        public T GetOrElse(T defaultValue)
        {
            if (IsSuccess)
            {
                return Value;
            }
            else
            {
                return defaultValue;
            }
        }

        public Try<T> OrElse(Func<Try<T>> func)
        {
            if (IsSuccess)
            {
                return this;
            }
            else
            {
                return func();
            }
        }

        public abstract Try<T> recover(Func<Exception, T> func);

        public abstract Try<T> recoverWith(Func<Exception, Try<T>> func);

        public IEnumerator<T> GetEnumerator()
        {
            if (IsSuccess)
            {
                yield return Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static Try<T> F<T>(Func<T> func)
        {
            try
            {
                return new Success<T>(func());
            }
            catch (Exception e)
            {
                return new Failure<T>(e);
            }
        }

        public static Try<T> Success(T value)
        {
            return new Success<T>(value);
        }

        public static Try<T> Failure(Exception e)
        {
            return new Failure<T>(e);
        }
    }

    public class Success<T> : Try<T>
    {
        internal Success(T value)
        {
            Value = value;
        }

        public override T Value { get; }

        public override bool IsFailure
        {
            get
            {
                return false;
            }
        }

        public override Try<T> recover(Func<Exception, T> func)
        {
            return this;
        }

        public override Try<T> recoverWith(Func<Exception, Try<T>> func)
        {
            return this;
        }
    }

    public class Failure<T> : Try<T>
    {
        public Failure(Exception e)
        {
            Exception = e;
        }

        public override T Value
        {
            get
            {
                throw new NoElementException();
            }
        }

        public override bool IsFailure
        {
            get
            {
                return true;
            }
        }

        public Exception Exception { get; }

        public override Try<T> recover(Func<Exception, T> func)
        {
            return Try<T>.F(() => func(Exception));
        }

        public override Try<T> recoverWith(Func<Exception, Try<T>> func)
        {
            return func(Exception);
        }
    }
}
