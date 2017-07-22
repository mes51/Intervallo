using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.WORLD
{
    class XorShift
    {
        // XorShift128+
        // see: http://xorshift.di.unimi.it/xorshift128plus.c
        ulong stage0 = 0x8a5cd789635d2dffUL;
        ulong stage1 = 0x121fd2155c472f96UL;

        public double Next()
        {
            var s1 = stage0;
            var s0 = stage1;
            var result = s1 + s0;

            stage0 = s0;
            s1 ^= s1 << 23;
            stage1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
            return result / (double)ulong.MaxValue;
        }
    }
}
