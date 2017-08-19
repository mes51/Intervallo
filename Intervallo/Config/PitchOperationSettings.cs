using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Config
{
    [DataContract]
    public class PitchOperationSettings
    {
        [DataMember(Name = "framePeriod")]
        public double FramePeriod { get; set; } = 5.0;
    }
}
