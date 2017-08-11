using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins
{
    static class ParallelF
    {
        static int processorCount = Environment.ProcessorCount - 1;

        public static void For(int fromInclusive, int toExclusive, Action<int> action)
        {
            if (processorCount < 2)
            {
                for (var i = fromInclusive; i < toExclusive; i++)
                {
                    action(i);
                }
            }
            else
            {
                ParallelFor(fromInclusive, toExclusive, action);
            }
        }

        static void ParallelFor(int fromInclusive, int toExclusive, Action<int> action)
        {
            int inc = fromInclusive - 1;
            Thread[] threads = new Thread[processorCount - 1];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    int ti = 0;
                    while (true)
                    {
                        ti = Interlocked.Increment(ref inc);
                        if (ti < toExclusive)
                        {
                            action(ti);
                        }
                        else
                        {
                            break;
                        }
                    }
                });
                threads[i].Start();
            }
            while (true)
            {
                int ti = Interlocked.Increment(ref inc);
                if (ti < toExclusive)
                {
                    action(ti);
                }
                else
                {
                    break;
                }
            }
            for (int i = 0; i < threads.Length; i++)
            {
                if (threads[i].IsAlive)
                {
                    threads[i].Join();
                }
            }
        }
    }
}
