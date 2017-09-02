using Intervallo.InternalUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.Vsqx
{
    public class Track
    {
        public Track(string name, RangeDictionary<double, Part> parts)
        {
            Name = name;
            Parts = parts;
        }

        public string Name { get; }

        public RangeDictionary<double, Part> Parts { get; }

        public IEnumerable<double> ToF0(double framePeriod)
        {
            var lastPart = Parts.Last().Value;
            var length = lastPart.TrackPosition + lastPart.PlayTime;

            Part currentPart = null;
            Note currentNote = null;
            IEnumerator<double> currentVibrato = null;
            for (var time = 0.0; time < length; time += framePeriod)
            {
                if (currentPart != Parts[time])
                {
                    currentPart = Parts[time];
                    currentNote = null;
                    currentVibrato?.Dispose();
                    currentVibrato = null;
                }
                if (time < currentPart.TrackPosition || time - currentPart.TrackPosition > currentPart.PlayTime)
                {
                    yield return 0.0;
                    continue;
                }

                var partTime = time - currentPart.TrackPosition;
                if (currentNote != currentPart.Note[partTime])
                {
                    currentNote = currentPart.Note[partTime];
                    currentVibrato?.Dispose();
                    currentVibrato = currentNote.Vibrato.CreateVibrato(currentNote.Length, framePeriod).Concat(EnumerableUtil.Infinity(0.0)).GetEnumerator();
                }
                if (partTime < currentNote.Position || partTime - currentNote.Position > currentNote.Length)
                {
                    yield return 0.0;
                    continue;
                }

                currentVibrato.MoveNext();
                yield return GetFrequency(currentNote.NoteNumber + currentVibrato.Current + currentPart.PitchBend[partTime]);
            }
        }

        double GetFrequency(double noteNumber)
        {
            return 440.0 * Math.Pow(2, (noteNumber - 69) / 12.0);
        }
    }

    public class Part
    {
        const double MaxPortamentoDelay = 0.25;

        public Part(double trackPosition, double playTime, RangeDictionary<double, Note> note, RangeDictionary<double, double> pitchBend, RangeDictionary<double, double> portamento)
        {
            TrackPosition = trackPosition;
            PlayTime = playTime;
            Note = note;
            PitchBend = pitchBend;
            Portamento = portamento;
        }

        public double TrackPosition { get; }

        public double PlayTime { get; }

        public RangeDictionary<double, Note> Note { get; }

        public RangeDictionary<double, double> PitchBend { get; }

        public RangeDictionary<double, double> Portamento { get; }
    }

    public class Tempo
    {
        public Tempo(int tick, double totalTime, double tempo, int resolution)
        {
            Tick = tick;
            TotalTime = totalTime;
            TickPerTime = 1.0 / (60.0 / tempo / resolution);
        }

        public int Tick { get; }

        public double TotalTime { get; }

        public double TickPerTime { get; }

        public int TimeToTick(double time)
        {
            return Tick + (int)((time - TotalTime) * TickPerTime);
        }

        public double TickToTime(int tick)
        {
            return TotalTime + (tick - Tick) / TickPerTime;
        }
    }

    public class Note
    {
        public Note(double position, double length, int noteNumber, Vibrato vibrato)
        {
            Position = position;
            Length = length;
            NoteNumber = noteNumber;
            Vibrato = vibrato;
        }

        public double Position { get; }

        public double Length { get; }

        public int NoteNumber { get; }

        public double VibratoLength { get; }

        public Vibrato Vibrato { get; }
    }

    public class Vibrato
    {
        public const double VibratoEdgeTime = 0.4;

        public Vibrato(int vibratoType, double vibratoLength, RangeDictionary<double, int> vibratoDepth, RangeDictionary<double, int> vibratoRate)
        {
            VibratoType = vibratoType;
            VibratoLength = vibratoLength;
            VibratoDepth = vibratoDepth;
            VibratoRate = vibratoRate;
        }

        public int VibratoType { get; }

        public double VibratoLength { get; }

        public RangeDictionary<double, int> VibratoDepth { get; }

        public RangeDictionary<double, int> VibratoRate { get; }

        public IEnumerable<double> CreateVibrato(double noteLength, double framePeriod)
        {
            var startTime = noteLength - VibratoLength;

            var waveStartTime = 0.0;
            var phase = Math.PI;
            for (var time = 0.0; time < noteLength; time += framePeriod)
            {

                var depth = VibratoDepth[time];
                var rate = VibratoRate[time];
                if (time < startTime || depth <= 0.0 || rate <= 0.0)
                {
                    phase = Math.PI;
                    waveStartTime = time;
                    yield return 0.0;
                }
                else
                {
                    yield return Math.Sin(phase) * depth * 0.01 * TanhWindowedValue(time, waveStartTime, noteLength, VibratoEdgeTime);
                    phase += Math.PI * (9.77952755905512 + (25.0 / 127.0) * rate) * 0.5 * framePeriod;
                }
            }
        }

        double TanhWindowedValue(double t, double begin, double end, double edge)
        {
            edge = Math.Min(edge, (end - begin) * 0.5);
            if (t - begin < edge)
            {
                return (1.0 + Math.Tanh(Math.PI * 2.0 * ((t - begin) / edge) - Math.PI)) * 0.5;
            }
            else if (end - t < edge)
            {
                return (1.0 + Math.Tanh(Math.PI * 2.0 * ((end - t) / edge) - Math.PI)) * 0.5;
            }
            else
            {
                return 1.0;
            }
        }
    }

    public class PortamentoInfo
    {
        const double BeginTick = 660.0;
        const double EndTick = 880.0;
        const double TotalPortamentoTime = EndTick + BeginTick;

        public const double MaxBeginTime = BeginTick * (60.0 / 120.0 / 960.0);
        public const double MaxEndTime = EndTick * (60.0 / 120.0 / 960.0);

        public PortamentoInfo(int portamento)
        {
            var beginMarginTick = 0.0;
            var endMarginTick = 0.0;
            var blendTick = 0.0;

            if (portamento >= 64)
            {
                beginMarginTick = BeginTick - 135.0 + Enumerable.Range(0, portamento - 64).Select((i) => 1.0 + 0.2385913 * i).Sum();
                blendTick = 280.0 + Enumerable.Range(0, portamento - 64).Select((i) => 1.0 + 0.0476191 * i).Sum();
            }
            else
            {
                beginMarginTick = BeginTick - 135.0 - Enumerable.Range(0, 64 - portamento).Select((i) => 1.0 + 0.2286706 * i).Sum();
                blendTick = 280.0;
            }
            endMarginTick = beginMarginTick + blendTick;

            BeginMarginTimeRate = beginMarginTick / TotalPortamentoTime;
            EndMarginTimeRate = (TotalPortamentoTime - endMarginTick) / TotalPortamentoTime;
        }

        public double BeginMarginTimeRate { get; }

        public double EndMarginTimeRate { get; }

        public double BlendTimeRate => 1.0 - (BeginMarginTimeRate + EndMarginTimeRate);
    }
}
