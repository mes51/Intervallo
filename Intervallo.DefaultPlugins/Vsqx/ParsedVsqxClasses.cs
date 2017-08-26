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
        public Track(string name, RangeDictionary<double, Tempo> tempo, RangeDictionary<int, Part> parts)
        {
            Name = name;
            Tempo = tempo;
            InvertTempo = tempo.Values.ToRangeDictionary((t) => t.Tick, IntervalMode.OpenInterval);
            Parts = parts;
        }

        public string Name { get; }

        public RangeDictionary<double, Tempo> Tempo { get; }

        public RangeDictionary<int, Tempo> InvertTempo { get; }

        public RangeDictionary<int, Part> Parts { get; }

        public double[] ToF0(int maxFrameLength, double framePeriod)
        {
            return Enumerable.Range(0, maxFrameLength)
                .Select((i) =>
                {
                    var time = i * framePeriod * 0.001;
                    var tick = Tempo[time].TimeToTick(time);
                    var part = Parts[tick];

                    if (tick < part.TrackPosition || tick - part.TrackPosition >= part.PlayTime)
                    {
                        return 0.0;
                    }

                    return GetF0(tick - part.TrackPosition, part);
                })
                .ToArray();
        }

        double GetF0(int tick, Part part)
        {
            var note = part.GetNote(tick);

            if (tick < note.Position || tick - note.Position > note.Length)
            {
                return 0.0;
            }

            if (note.VibratoResult == null)
            {
                note.VibratoResult = CreateVibrato(note);
            }

            /*
            var prev = part.GetPrevNote(tick);
            var next = part.GetNextNote(tick);

            var portamento = new PortamentoInfo(part.Portamento[note.Position]);
            var nextPortamento = new PortamentoInfo(part.Portamento[next.Position]);
            */
            var blended = note.NoteNumber + note.VibratoResult[tick - note.Position];

            return GetFrequency(blended + part.PitchBend[tick]);
        }

        RangeDictionary<int, double> CreateVibrato(Note note)
        {
            if (note.VibratoType == 0 || note.VibratoLength < 1)
            {
                return new RangeDictionary<int, double>(IntervalMode.OpenInterval, new Dictionary<int, double>()
                {
                    [0] = 0.0
                });
            }

            var tickRate = (1 << 16) / (double)note.Length;
            var startTick = (int)(note.Length * (100 - note.VibratoLength) * 0.01);
            var endTime = InvertTempo[note.Position + note.Length].TickToTime(note.Position + note.Length);
            var waveStartTime = InvertTempo[note.Position + startTick].TickToTime(note.Position + startTick);

            var result = new RangeDictionary<int, double>(IntervalMode.OpenInterval);
            var phase = Math.PI;
            for (var i = startTick; i < note.Length; i++)
            {
                var pos = note.Position + i;
                var time = InvertTempo[pos].TickToTime(pos);

                var depth = note.VibratoDepth[(int)(tickRate * i)];
                var rate = note.VibratoRate[(int)(tickRate * i)];
                if (depth < 1 || rate < 1)
                {
                    result.Add(i, 0.0);
                    phase = Math.PI;
                    waveStartTime = time;
                }
                else
                {
                    result.Add(i, Math.Sin(phase) * depth * 0.01 * TanhWindowedValue(time, waveStartTime, endTime, Note.VibratoEdgeTime));
                    var nextTime = InvertTempo[pos + 1].TickToTime(pos + 1);
                    phase += Math.PI * (9.77952755905512 + (25.0 / 127.0) * rate) * 0.5 * (time - nextTime);
                }
            }

            return result;
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

        double GetFrequency(double noteNumber)
        {
            return 440.0 * Math.Pow(2, (noteNumber - 69) / 12.0);
        }
    }

    public class Part
    {
        const double MaxPortamentoDelay = 0.25;

        public Part(int trackPosition, int playTime, RangeDictionary<int, Note> note, RangeDictionary<int, double> pitchBend, RangeDictionary<int, int> portamento)
        {
            TrackPosition = trackPosition;
            PlayTime = playTime;
            Note = note;
            PitchBend = pitchBend;
            Portamento = portamento;
        }

        public int TrackPosition { get; }

        public int PlayTime { get; }

        public RangeDictionary<int, Note> Note { get; }

        public RangeDictionary<int, double> PitchBend { get; }

        public RangeDictionary<int, int> Portamento { get; }

        public Note GetNote(int tick)
        {
            return Note[tick];
        }

        public Note GetPrevNote(int tick)
        {
            var key = Note.SelectKey(tick);
            return Note[key.Value - 1];
        }

        public Note GetNextNote(int tick)
        {
            var key = Note.SelectKey(tick).Value;
            return Note[Note.Keys.SkipWhile((k) => k != key).Take(2).Last()];
        }

        public double GetF0(int tick)
        {
            if (tick > -1 && tick < PlayTime)
            {
                var note = Note[tick];
                if (tick - note.Position < note.Length)
                {
                    var key = Note.SelectKey(tick);
                    var prev = Note[key.Value - 1];
                    var next = Note[key.Value + note.Length];
                    return 0;//note.GetFrequency(PitchBend[tick]);
                }
            }

            return 0.0;
        }
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
        public const double VibratoEdgeTime = 0.4;

        public Note(int position, int length, int noteNumber, double vibratoLength, int vibratoType, RangeDictionary<int, int> vibratoDepth, RangeDictionary<int, int> vibratoRate)
        {
            Position = position;
            Length = length;
            NoteNumber = noteNumber;
            VibratoLength = vibratoLength;
            VibratoType = vibratoType;
            VibratoDepth = vibratoDepth;
            VibratoRate = vibratoRate;
        }

        public int Position { get; }

        public int Length { get; }

        public int NoteNumber { get; }

        public double VibratoLength { get; }

        public int VibratoType { get; }

        public RangeDictionary<int, int> VibratoDepth { get; }

        public RangeDictionary<int, int> VibratoRate { get; }

        public RangeDictionary<int, double> VibratoResult { get; set; }
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
