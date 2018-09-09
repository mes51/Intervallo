using Intervallo.InternalUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins.Vocaloid
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

        public IEnumerable<double> ToF0(double framePeriod, bool addFluctuation)
        {
            var lastPart = Parts.Last().Value;
            var length = lastPart.TrackPosition + lastPart.PlayTime;

            Part currentPart = null;
            Note currentNote = null;
            IEnumerator<double> curve = null;
            using (var noise = CreateNoise(framePeriod).GetEnumerator())
            {
                for (var time = 0.0; time < length; time += framePeriod)
                {
                    if (currentPart != Parts[time])
                    {
                        currentPart = Parts[time];
                        currentNote = null;
                        curve?.Dispose();
                        curve = null;
                    }
                    if (time < currentPart.TrackPosition || time - currentPart.TrackPosition > currentPart.PlayTime || !currentPart.HasNote())
                    {
                        yield return 0.0;
                        continue;
                    }

                    var partTime = time - currentPart.TrackPosition;
                    if (currentNote != currentPart.GetNote(partTime))
                    {
                        currentNote = currentPart.GetNote(partTime);
                        curve?.Dispose();
                        curve = currentPart.CreateCurve(partTime, framePeriod).GetEnumerator();
                    }
                    if (partTime < currentNote.Position || partTime - currentNote.Position > currentNote.Length)
                    {
                        yield return 0.0;
                        continue;
                    }

                    curve.MoveNext();

                    var ni = curve.Current + currentPart.PitchBend[partTime];
                    if (addFluctuation)
                    {
                        noise.MoveNext();
                        ni += noise.Current;
                    }
                    yield return GetFrequency(ni);
                }
            }
        }

        double GetFrequency(double noteNumber)
        {
            return 440.0 * Math.Pow(2, (noteNumber - 69) / 12.0);
        }

        IEnumerable<double> CreateNoise(double framePeriod)
        {
            const double interval = 0.1;
            var rand = new Random(unchecked((int)504463712879));
            var p1 = 0.0;
            var c = 0.0;
            var n1 = (rand.NextDouble() - 0.5) * 0.02;
            var n2 = (rand.NextDouble() - 0.5) * 0.02;
            var pt = 0.0;
            var nt = interval;
            var t = 0.0;

            while (true)
            {
                yield return CatmullRom(p1, c, n1, n2, pt, nt, t);

                t += framePeriod;
                if (t >= nt)
                {
                    pt = nt;
                    nt += interval;
                    p1 = c;
                    c = n1;
                    n1 = n2;
                    n2 = (rand.NextDouble() - 0.5) * 0.2;
                }
            }
        }

        static double CatmullRom(double value0, double value1, double value2, double value3, double time1, double time2, double time)
        {
            double t = 1.0 / (time2 - time1) * (time - time1);
            double v0 = (value2 - value0) / 2.0;
            double v1 = (value3 - value1) / 2.0;
            double t2 = t * t;
            double t3 = t2 * t;
            return (2 * value1 - 2 * value2 + v0 + v1) * t3 + (-3 * value1 + 3 * value2 - 2 * v0 - v1) * t2 + v0 * t + value1;
        }
    }

    public class Part
    {
        const double MaxPortamentoDelay = 0.25;

        public Part(double trackPosition, double playTime, RangeDictionary<double, Note> note, RangeDictionary<double, double> pitchBend)
        {
            TrackPosition = trackPosition;
            PlayTime = playTime;
            Note = note;
            PitchBend = pitchBend;
        }

        public double TrackPosition { get; }

        public double PlayTime { get; }

        public RangeDictionary<double, Note> Note { get; }

        public RangeDictionary<double, double> PitchBend { get; }

        public bool HasNote()
        {
            return Note.Count > 0;
        }

        public Note GetNote(double time)
        {
            return Note[time];
        }

        public IEnumerable<double> CreateCurve(double time, double framePeriod)
        {
            var note = GetNote(time);

            var f0 = new double[(int)Math.Ceiling(note.Length / framePeriod)];
            f0.Fill(note.NoteNumber);

            GetPrevNote(time).ForEach((p) => ApplyPortamento(note, p, f0, framePeriod, false));
            GetNextNote(time).ForEach((n) => ApplyPortamento(note, n, f0, framePeriod, true));

            return f0.Zip(note.Vibrato.CreateVibrato(note.Length, framePeriod), (n, v) => n + v);
        }

        Optional<Note> GetNextNote(double time)
        {
            var key = Note.SelectKey(time).Value;
            return Note.Keys
                .SkipWhile((k) => k != key)
                .Skip(1)
                .FirstOption()
                .Select((k) => Note[k]);
        }

        Optional<Note> GetPrevNote(double time)
        {
            var key = Note.SelectKey(time).Value;
            return Note.Keys
                .SkipPrevWhile((k) => k != key)
                .FirstOption()
                .Where((k) => k != time)
                .Select((k) => Note[k]);
        }

        void ApplyPortamento(Note note, Note affector, double[] f0, double framePeriod, bool reverse)
        {
            var positionAdjustment = framePeriod - note.Position % framePeriod;

            if (reverse)
            {
                var tmp = note;
                note = affector;
                affector = tmp;
            }

            if (note.Position - (affector.Position + affector.Length) > affector.Portamento.GapToleranceTime)
            {
                return;
            }

            var prevTimeRate = Math.Min(affector.Length * 0.5 / Portamento.MaxPrevNoteTime, 1.0);
            var currentTimeRate = Math.Min(Math.Min(note.Length, affector.Length) * 0.5 / Portamento.MaxNextNoteTime, 1.0);
            var startTime = Math.Min(Portamento.CenterTime, affector.Portamento.BeginMarginTimeRate) * prevTimeRate + Math.Max(0.0, affector.Portamento.BeginMarginTimeRate - Portamento.CenterTime) * currentTimeRate;
            var blendTime = affector.Portamento.BlendTimeRate * currentTimeRate; //Math.Max(startTime, 0.0) * prevTimeRate + Math.Max(affector.Portamento.BlendTimeRate - Math.Max(startTime, 0.0), 0.0) * currentTimeRate;
            if (blendTime < affector.Portamento.BlendTimeRate)
            {
                var diff = (Math.Min(Math.Min(note.Length, affector.Length), affector.Portamento.BlendTimeRate) - blendTime) * affector.Portamento.CutTolerance;
                if (diff > 0.0)
                {
                    blendTime += diff;
                    startTime += diff * 0.5;
                }
            }
            var rad = (Math.PI * 2.0) / blendTime;
            blendTime += Math.Max(note.Position - (affector.Position + affector.Length), 0.0);

            if (!reverse)
            {
                for (var i = 0; i < f0.Length; i++)
                {
                    var t = Math.Max(startTime + positionAdjustment + i * framePeriod, 0.0);
                    if (t >= blendTime)
                    {
                        break;
                    }
                    var blendRate = CurveFunction(rad * t);
                    f0[i] = affector.NoteNumber + (note.NoteNumber - affector.NoteNumber) * blendRate;
                }
            }
            else
            {
                for (var i = 0; i < f0.Length; i++)
                {
                    var t = startTime - (positionAdjustment + i * framePeriod);
                    if (t < 0.0)
                    {
                        break;
                    }
                    var blendRate = CurveFunction(rad * t);
                    f0[f0.Length - i - 1] = affector.NoteNumber + (note.NoteNumber - affector.NoteNumber) * blendRate;
                }
            }
        }

        double CurveFunction(double rad)
        {
            // see: https://ja.wikipedia.org/wiki/%E3%82%B0%E3%83%BC%E3%83%87%E3%83%AB%E3%83%9E%E3%83%B3%E9%96%A2%E6%95%B0
            const double GPPI = 0.9450125422; // gp(PI) * 2 / PI
            return Math.Min(1.0, (1.0 + Math.Atan(Math.Sinh(rad - Math.PI)) * 2 / Math.PI / GPPI) * 0.5);
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
        public Note(string character, double position, double length, int noteNumber, Vibrato vibrato, Portamento portamento)
        {
            Character = character;
            Position = position;
            Length = length;
            NoteNumber = noteNumber;
            Vibrato = vibrato;
            Portamento = portamento;
        }

        public string Character { get; }

        public double Position { get; }

        public double Length { get; }

        public int NoteNumber { get; }

        public double VibratoLength { get; }

        public Vibrato Vibrato { get; }

        public Portamento Portamento { get; }
    }

    // TODO: Support V5
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

    public class Portamento
    {
        const double Resolution = 960.0;
        const double BaseBPM = 120.0;
        const double TickToTime = 60.0 / BaseBPM / Resolution;

        const double BeginTick = 660.0;
        const double EndTick = 880.0;
        const double CenterTick = 165.0;

        const double Sum0To63 = 2016.0;
        const double BeginMarginGTE64 = 511.0 / Sum0To63;
        const double BeginMarginLT64 = 101.0 / Sum0To63;
        const double BlendTimeGTE64 = 100.0 / Sum0To63;
        const double BlendTimeLT64 = -60.0 / Sum0To63;
        const double GapToleranceInterval = 660.0 / Sum0To63;

        public const double CenterTime = CenterTick * TickToTime;
        public const double MaxBeginTime = BeginTick * TickToTime;
        public const double MaxEndTime = EndTick * TickToTime;
        public const double MaxPrevNoteTime = 0.5 - MaxBeginTime;
        public const double MaxNextNoteTime = MaxEndTime;

        public Portamento(int portamento)
        {
            var beginMarginTick = 0.0;
            var blendTick = 340.0;
            var diff = Math.Abs(portamento - 64);
            if (portamento >= 64)
            {
                beginMarginTick = CenterTick + Enumerable.Range(0, diff).Select((i) => 1.0 + BeginMarginGTE64 * i).Sum();
                blendTick += Enumerable.Range(0, diff).Select((i) => 1.0 + BlendTimeGTE64 * i).Sum();
            }
            else
            {
                beginMarginTick = Math.Max(CenterTick - Enumerable.Range(0, diff).Select((i) => 1.0 + BeginMarginLT64 * i).Sum(), 0.0);
            }

            BeginMarginTimeRate = beginMarginTick * TickToTime;
            BlendTimeRate = blendTick * TickToTime;
            GapToleranceTime = (60.0 + Enumerable.Range(0, diff).Select((i) => 1.0 + GapToleranceInterval * i).Sum()) * TickToTime;
            CutTolerance = 1.0 - diff / 64.0;
        }

        public double BeginMarginTimeRate { get; }

        public double BlendTimeRate { get; }

        public double GapToleranceTime { get; }

        public double CutTolerance { get; }
    }
}
