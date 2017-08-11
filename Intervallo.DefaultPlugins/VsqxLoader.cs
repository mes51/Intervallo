using Intervallo.DefaultPlugins;
using Intervallo.DefaultPlugins.Properties;
using Intervallo.DefaultPlugins.Vsqx;
using Intervallo.Plugin;
using Intervallo.Plugin.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Intervallo.DefaultPlugins
{
    [Export(typeof(IScaleLoader))]
    public class VsqxLoader : IScaleLoader
    {
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResource.VsqxLoader_Description;

        public string PluginName => typeof(VsqxLoader).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public string[] SupportedFileExtensions => new string[] { "*.vsqx" };

        public double[] Load(string filePath, double framePeriod, int maxFrameLength)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    return LoadFromVsq4(fs, framePeriod).First().ToF0(maxFrameLength, framePeriod);
                }
            }
            catch (Exception e)
            {
                throw new ScaleLoadException(LangResource.VsqLoader_FailLoadFile, e);
            }
        }

        Track[] LoadFromVsq4(FileStream fs, double framePeriod)
        {
            var serializer = new XmlSerializer(typeof(vsq4));
            var vsq = serializer.Deserialize(fs) as vsq4;

            var resolution = vsq.masterTrack.resolution;
            var measureTicks = resolution * 4; // for 4/4
            var preMeasureTicks = Enumerable.Range(0, vsq.masterTrack.preMeasure)
                .Select((i) => vsq.masterTrack.timeSig.First((sig) => sig.m <= i))
                .Select((sig) => measureTicks / sig.de * sig.nu)
                .Sum();

            var firstTempo = vsq.masterTrack.tempo
                .TakeWhile((t) => t.t <= preMeasureTicks)
                .Last();
            firstTempo.t = preMeasureTicks;
            var tempo = firstTempo
                .PushTo(vsq.masterTrack.tempo.SkipWhile((t) => t.t > preMeasureTicks))
                .SelectReferencePrev<tempo, Tempo>((v, p) =>
                    new Tempo(v.t - preMeasureTicks, p.Select((pt) => pt.TotalTime + (v.t - preMeasureTicks - pt.Tick) / pt.TickPerTime).FirstOrDefault(), v.v * 0.01, resolution)
                )
                .ToRangeDictionary((v) => v.TotalTime, IntervalMode.OpenInterval);

            return vsq.vsTrack.Select((t) =>
            {
                var parts = t.vsPart
                    .Select((p) =>
                    {
                        var notes = p.note
                            .Select((n) => new Note(n.t, n.dur, n.n))
                            .ToRangeDictionary((n) => n.Position, IntervalMode.OpenInterval);

                        var controls = (p.cc ?? new cc[0]).TakeWhile((c) => c.t < p.playTime).ToArray();

                        var pbs = controls.Where((c) => c.v.id == "S")
                            .ToRangeDictionary((c) => c.t, (c) => c.v.Value, IntervalMode.OpenInterval);
                        if (pbs.Count < 1)
                        {
                            pbs.Add(0, 2);
                        }
                        var pitchBend = controls.Where((c) => c.v.id == "P")
                            .ToRangeDictionary((c) => c.t, (c) => c.v.Value / 8192.0 * pbs[c.t], IntervalMode.OpenInterval);
                        if (pitchBend.Count < 1)
                        {
                            pitchBend.Add(0, 0.0);
                        }

                        var portamento = controls.Where((c) => c.v.id == "T")
                            .ToRangeDictionary((c) => c.t, (c) => c.v.Value, IntervalMode.OpenInterval);
                        if (portamento.Count < 1)
                        {
                            portamento.Add(0, 64);
                        }

                        return new Part(p.t - preMeasureTicks, p.playTime, notes, pitchBend, portamento);
                    })
                    .ToRangeDictionary((p) => p.TrackPosition, IntervalMode.OpenInterval);

                return new Track(tempo, parts);
            }).ToArray();
        }
    }

    class Track
    {
        public Track(RangeDictionary<double, Tempo> tempo, RangeDictionary<int, Part> parts)
        {
            Tempo = tempo;
            Parts = parts;
        }

        public RangeDictionary<double, Tempo> Tempo { get; }

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

            var prev = part.GetPrevNote(tick);
            var next = part.GetNextNote(tick);

            var portamento = new PortamentoInfo(part.Portamento[note.Position]);
            var nextPortamento = new PortamentoInfo(part.Portamento[next.Position]);
            





            var blended = note.NoteNumber;

            return GetFrequency(blended + part.PitchBend[tick]);
        }

        double GetFrequency(double noteNumber)
        {
            return 440.0 * Math.Pow(2, (noteNumber - 69) / 12.0);
        }
    }

    class Part
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

    class Tempo
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
    }

    class Note
    {
        public Note(int position, int length, int noteNumber)
        {
            Position = position;
            Length = length;
            NoteNumber = noteNumber;
        }

        public int Position { get; }

        public int Length { get; }

        public int NoteNumber { get; }
    }

    class PortamentoInfo
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
