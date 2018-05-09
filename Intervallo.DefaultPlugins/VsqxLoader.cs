using Intervallo.DefaultPlugins;
using Intervallo.DefaultPlugins.Properties;
using Intervallo.DefaultPlugins.Vsqx;
using Intervallo.Plugin;
using Intervallo.InternalUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Intervallo.DefaultPlugins.Form;
using System.Windows;

namespace Intervallo.DefaultPlugins
{
    [Export(typeof(IScaleLoader))]
    public class VsqxLoader : IScaleLoader
    {
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResources.VsqxLoader_Description;

        public string PluginName => typeof(VsqxLoader).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public string[] SupportedFileExtensions => new string[] { "*.vsqx" };

        public double[] Load(string filePath, double framePeriod, int maxFrameLength)
        {
            framePeriod *= 0.001;
            Track[] tracks = null;
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    tracks = LoadFromVsq4(fs);
                }
            }
            catch (Exception e)
            {
                throw new ScaleLoadException(LangResources.VsqLoader_FailLoadFile, e);
            }

            if (tracks.Length > 1)
            {
                double[] f0 = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var selectWindow = new VsqxTrackSelectWindow();
                    selectWindow.Tracks = tracks;
                    selectWindow.ShowDialog();

                    if (selectWindow.Selected)
                    {
                        f0 = selectWindow.SelectedTrack.ToF0(framePeriod)
                            .Concat(EnumerableUtil.Infinity(0.0))
                            .Take(maxFrameLength).ToArray();
                    }
                });

                if (f0 != null)
                {
                    return f0;
                }
                else
                {
                    throw new ScaleLoadException(LangResources.VsqLoader_CancelLoad);
                }
            }
            else
            {
                return tracks[0].ToF0(framePeriod)
                    .Concat(EnumerableUtil.Infinity(0.0))
                    .Take(maxFrameLength).ToArray();
            }
        }

        Track[] LoadFromVsq4(FileStream fs)
        {
            var serializer = new XmlSerializer(typeof(vsq4));
            return Parse(serializer.Deserialize(fs) as vsq4);
        }

        Track[] Parse(IVsqx vsq)
        {
            var resolution = vsq.MasterTrack.Resolution;
            var measureTicks = resolution * 4; // for 4/4
            var preMeasureTicks = Enumerable.Range(0, vsq.MasterTrack.PreMeasure)
                .Select((i) => vsq.MasterTrack.TimeSig.First((sig) => sig.Measure <= i))
                .Select((sig) => measureTicks / sig.Denominator * sig.Nume)
                .Sum();

            var firstTempo = vsq.MasterTrack.Tempo.First().PushTo(vsq.MasterTrack.Tempo.TakeWhile((t) => t.Tick <= preMeasureTicks)).Last();
            firstTempo.Tick = preMeasureTicks;
            var tempo = vsq.MasterTrack.Tempo.SkipWhile((t) => t.Tick < preMeasureTicks)
                .SelectReferencePrev<IVSTempo, Tempo>((v, p) =>
                    new Tempo(v.Tick - preMeasureTicks, p.Select((pt) => pt.TotalTime + (v.Tick - preMeasureTicks - pt.Tick) / pt.TickPerTime).FirstOrDefault(), v.BPM * 0.01, resolution)
                )
                .ToRangeDictionary((v) => v.Tick, IntervalMode.OpenInterval);

            return vsq.VSTrack.Select((t) =>
            {
                var parts = Optional<IVSPart[]>.FromNull(t.Part)
                    .SelectMany(_ => _)
                    .Select((p) =>
                    {
                        var partTick = p.Tick - preMeasureTicks;
                        var partStartTime = tempo[partTick].TickToTime(partTick);

                        var pbs = GetControlChange(p, "S", 2.0, partTick, tempo);
                        var pitchBend = GetControlChange(p, "P", 0.0, partTick, tempo)
                            .ToRangeDictionary((e) => e.Key, e => e.Value / 8192.0 * pbs[e.Key], IntervalMode.OpenInterval);
                        var portamento = GetControlChange(p, "T", 64.0, partTick, tempo);

                        var notes = p.Note
                            .Select((n) =>
                            {
                                var tick = partTick + n.Tick;
                                var time = tempo[tick].TickToTime(tick);
                                var length = tempo[tick + n.Duration].TickToTime(tick + n.Duration) - time;
                                var pot = new Portamento((int)portamento[tempo[tick + n.Duration].TickToTime(tick + n.Duration)]);
                                return new Note(n.Character, time - partStartTime, length, n.NoteNumber, GetVibratoInfo(n, partTick, length, tempo), pot);
                            })
                            .ToRangeDictionary((n) => n.Position, IntervalMode.OpenInterval);

                        return new Part(partStartTime, tempo[partTick + p.PlayTime].TickToTime(partTick + p.PlayTime) - partStartTime, notes, pitchBend);
                    })
                    .ToRangeDictionary((p) => p.TrackPosition, IntervalMode.OpenInterval);

                return new Track(t.Name, parts);
            }).ToArray();
        }

        Vibrato GetVibratoInfo(IVSNote note, int partTick, double noteLength, RangeDictionary<int, Tempo> tempo)
        {
            var vibDepth = GetNoteStyle(note, "vibDep", partTick, tempo);
            var vibRate = GetNoteStyle(note, "vibRate", partTick, tempo);
            var vibLength = note.NStyle.Attrs.FirstOrDefault((st) => st.ID == "vibLen")?.Value ?? 0;
            var vibType = note.NStyle.Attrs.FirstOrDefault((st) => st.ID == "vibType")?.Value ?? 0;

            return new Vibrato(vibType, vibLength * 0.01 * noteLength, vibDepth, vibRate);
        }

        RangeDictionary<double, int> GetNoteStyle(IVSNote note, string styleName, int partTick, RangeDictionary<int, Tempo> tempo)
        {
            var partStartTime = tempo[partTick].TickToTime(partTick);
            var styleTickRate = note.Duration / (double)(1 << 16);
            return note.NStyle.Sequence?
                .FirstOrDefault((s) => s.ID == styleName)?.CC
                .Select((c) => new { Tick = partTick + (int)(c.Position * styleTickRate), Value = c.Value  })
                .ToRangeDictionary((c) => tempo[c.Tick].TickToTime(c.Tick) - partStartTime, (c) => c.Value, IntervalMode.OpenInterval)
                ?? new RangeDictionary<double, int>(IntervalMode.OpenInterval, new Dictionary<double, int>() { [0.0] = 0 });
        }

        RangeDictionary<double, double> GetControlChange(IVSPart part, string id, double defaultValue, int partTick, RangeDictionary<int, Tempo> tempo)
        {
            var partStartTime = tempo[partTick].TickToTime(partTick);
            var result = (part.CC ?? new IVSControlChange[0])
                .TakeWhile((c) => c.Tick < part.PlayTime)
                .Where((c) => c.Attr.ID == id)
                .ToRangeDictionary((c) => tempo[partTick + c.Tick].TickToTime(partTick + c.Tick) - partStartTime, (c) => (double)c.Attr.Value, IntervalMode.OpenInterval);
            if (result.Count < 1 || !result.ContainsKey(0.0))
            {
                result.Add(0.0, defaultValue);
            }

            return result;
        }
    }
}
