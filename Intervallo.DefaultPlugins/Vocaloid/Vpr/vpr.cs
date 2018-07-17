using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.Vocaloid.Vpr
{
    [DataContract]
    public class Vpr
    {
        [DataMember(Name = "masterTrack")]
        public VprMasterTrack MasterTrack { get; set; }

        [DataMember(Name = "tracks")]
        public VprTrack[] Tracks { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }
    }

    [DataContract]
    public class VprMasterTrack
    {
        [DataMember(Name = "tempo")]
        public VprTempo Tempo { get; set; }

        [DataMember(Name = "timeSig")]
        public VprTimeSig TimeSig { get; set; }
    }

    [DataContract]
    public class VprTempo
    {
        [DataMember(Name = "global")]
        public VprGlobalTempo Global { get; set; }

        [DataMember(Name = "events")]
        public VprValue[] Events { get; set; }
    }

    [DataContract]
    public class VprGlobalTempo
    {
        [DataMember(Name = "isEnabled")]
        public bool IsEnabled { get; set; }

        [DataMember(Name = "value")]
        public int Value { get; set; }
    }

    [DataContract]
    public class VprTimeSig
    {
        [DataMember(Name = "events")]
        public VprTimeSigEvent[] Events { get; set; }
    }

    [DataContract]
    public class VprTimeSigEvent
    {
        [DataMember(Name = "bar")]
        public int Measure { get; set; }

        [DataMember(Name = "numer")]
        public int Numerator { get; set; }

        [DataMember(Name = "denom")]
        public int Denominator { get; set; }
    }

    [DataContract]
    public class VprTrack
    {
        [DataMember(Name = "type")]
        public int Type { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "parts")]
        public VprPart[] Parts { get; set; }
    }

    [DataContract]
    public class VprPart
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "pos")]
        public int Pos { get; set; }

        [DataMember(Name = "duration")]
        public int Duration { get; set; }

        [DataMember(Name = "notes")]
        public VprNote[] Notes { get; set; }

        [DataMember(Name = "controllers")]
        public VprController[] Controllers { get; set; }
    }

    [DataContract]
    public class VprNote
    {
        [DataMember(Name = "lyric")]
        public string Lyric { get; set; }

        [DataMember(Name = "pos")]
        public int Pos { get; set; }

        [DataMember(Name = "duration")]
        public int Duration { get; set; }

        [DataMember(Name = "number")]
        public int NoteNumber { get; set; }

        [DataMember(Name = "vibrato")]
        public VprVibrato Vibrato { get; set; }
    }

    [DataContract]
    public class VprVibrato
    {
        [DataMember(Name = "type")]
        public int Type { get; set; }

        [DataMember(Name = "duration")]
        public int Duration { get; set; }

        [DataMember(Name = "depths")]
        public VprValue[] Depths { get; set; }

        [DataMember(Name = "rates")]
        public VprValue[] Rates { get; set; }
    }

    [DataContract]
    public class VprController
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "events")]
        public VprValue[] Events { get; set; }
    }

    [DataContract]
    public class VprValue
    {
        [DataMember(Name = "pos")]
        public int Pos { get; set; }

        [DataMember(Name = "value")]
        public int Value { get; set; }
    }
}
