using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Util
{
    public class NoElementException : Exception
    {
        public NoElementException() { }

        public NoElementException(string message) : base(message) { }

        public NoElementException(string message, Exception inner) : base(message, inner) { }
    }
}
