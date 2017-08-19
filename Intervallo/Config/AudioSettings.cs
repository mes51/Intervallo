using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Config
{
    [DataContract]
    public class AudioSettings
    {
        [DataMember(Name = "previewLatency")]
        public int PreviewLatency { get; set; } = 200;
    }
}
