using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    public class LoopRange
    {
        public LoopRange(int beginSample, int endSample)
        {
            BeginSample = beginSample;
            EndSample = endSample;
        }

        public int BeginSample { get; }

        public int EndSample { get; }
    }
}
