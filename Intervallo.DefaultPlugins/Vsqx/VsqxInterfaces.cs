using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.Vsqx
{
    public interface IVsqx
    {
        IVSMasterTrack MasterTrack { get; }

        IVSTrack[] VSTrack { get; }
    }

    public interface IVSMasterTrack
    {
        ushort Resolution { get; }

        byte PreMeasure { get; }

        IVSTimeSig[] TimeSig { get; }

        IVSTempo[] Tempo { get; }
    }

    public interface IVSTimeSig
    {
        int Measure { get; }

        byte Nume { get; }

        byte Denominator { get; }
    }

    public interface IVSTempo
    {
        int Tick { get; set; }

        int BPM { get; }
    }

    public interface IVSTrack
    {
        string Name { get; }

        IVSPart[] Part { get; }
    }

    public interface IVSPart
    {
        int Tick { get; }

        int PlayTime { get; }

        IVSControlChange[] CC { get; }

        IVSNote[] Note { get; }
    }

    public interface IVSControlChange
    {
        int Tick { get; }

        IVSTypeParamAttr Attr { get; }
    }

    public interface IVSTypeParamAttr
    {
        string ID { get; }

        int Value { get; }
    }

    public interface IVSNote
    {
        int Tick { get; }

        int Duration { get; }

        int NoteNumber { get; }

        IVSNStyle NStyle { get; }
    }

    public interface IVSNStyle
    {
        IVSTypeParamAttr[] Attrs { get; }

        IVSSeq[] Sequence { get; }
    }

    public interface IVSSeq
    {
        IVSSeqControlChange[] CC { get; }

        string ID { get; }
    }

    public interface IVSSeqControlChange
    {
        int Position { get; }

        int Value { get; }
    }
}
