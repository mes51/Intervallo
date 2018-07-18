using Intervallo.DefaultPlugins.Form;
using Intervallo.DefaultPlugins.Properties;
using Intervallo.DefaultPlugins.Vocaloid;
using Intervallo.DefaultPlugins.Vocaloid.Vpr;
using Intervallo.InternalUtil;
using Intervallo.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Intervallo.DefaultPlugins
{
    [Export(typeof(IScaleLoader))]
    public class VprLoader : IScaleLoader
    {
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResources.VprLoader_Description;

        public string PluginName => typeof(VprLoader).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public string[] SupportedFileExtensions => new string[] { "*.vpr" };

        public double[] Load(string filePath, double framePeriod, int maxFrameLength)
        {
            framePeriod *= 0.001;
            var tracks = LoadFile(filePath);
            if (tracks.Length < 1)
            {
                throw new ScaleLoadException(LangResources.VprLoader_TrackNotFound);
            }
            else if (tracks.Length > 1)
            {
                double[] f0 = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var selectWindow = new TrackSelectWindow();
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

        Track[] LoadFile(string file)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(file))
                {
                    var entry = archive.GetEntry("Project\\sequence.json");
                    if (entry != null)
                    {
                        using (var stream = entry.Open())
                        {
                            var serializer = new DataContractJsonSerializer(typeof(Vpr));
                            if (serializer.ReadObject(stream) is Vpr vpr)
                            {
                                return Parse(vpr);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ScaleLoadException(LangResources.VprLoader_FailLoadFile, e);
            }

            throw new ScaleLoadException(LangResources.VprLoader_FailLoadFile);
        }

        Track[] Parse(Vpr vpr)
        {
            const int resolution = 480;

            IEnumerable<VprValue> vprTempo = vpr.MasterTrack.Tempo.Events;
            if ((vprTempo.OrderBy(t => t.Pos).FirstOrDefault()?.Pos ?? int.MaxValue) != 0)
            {
                vprTempo = new VprValue { Pos = 0, Value = vpr.MasterTrack.Tempo.Global.Value }.PushTo(vprTempo);
            }
            var tempo = vprTempo
                .SelectReferencePrev<VprValue, Tempo>((v, p) => new Tempo(v.Pos, p.Select((pt) => pt.TotalTime + v.Pos / pt.TickPerTime).FirstOrDefault(), v.Value * 0.01, resolution))
                .ToRangeDictionary(v => v.Tick, IntervalMode.OpenInterval);

            return vpr.Tracks.Where(t => t.Type == 0)
                .Select(t =>
            {
                var parts = Optional<VprPart[]>.FromNull(t.Parts)
                    .SelectMany(_ => _)
                    .Select(p =>
                    {
                        var partTick = p.Pos;
                        var partStartTime = tempo[partTick].TickToTime(partTick);

                        var pbs = GetControlChange(p, "pitchBendSens", 2.0, partTick, tempo);
                        var pitchBend = GetControlChange(p, "pitchBend", 0.0, partTick, tempo)
                            .ToRangeDictionary(e => e.Key, e => e.Value / 8192.0 * pbs[e.Key], IntervalMode.OpenInterval);
                        var portamento = GetControlChange(p, "portamento", 64.0, partTick, tempo);

                        var notes = p.Notes
                            .Select(n =>
                            {
                                var tick = partTick + n.Pos;
                                var time = tempo[tick].TickToTime(tick);
                                var length = tempo[tick + n.Duration].TickToTime(tick + n.Duration) - time;
                                var pot = new Portamento((int)portamento[tempo[tick + n.Duration].TickToTime(tick + n.Duration)]);
                                return new Note(n.Lyric, time - partStartTime, length, n.NoteNumber, GetVibratoInfo(n, partTick, tempo), pot);
                            })
                            .ToRangeDictionary((n) => n.Position, IntervalMode.OpenInterval);

                        return new Part(partStartTime, tempo[partTick + p.Duration].TickToTime(partTick + p.Duration) - partStartTime, notes, pitchBend);
                    })
                    .ToRangeDictionary((p) => p.TrackPosition, IntervalMode.OpenInterval);

                return new Track(t.Name, parts);
            }).ToArray();
        }

        Vibrato GetVibratoInfo(VprNote note, int partTick, RangeDictionary<int, Tempo> tempo)
        {
            var vibrato = note.Vibrato;
            if (vibrato.Depths == null || vibrato.Rates == null)
            {
                return new Vibrato(0, 0.0, new RangeDictionary<double, int>(IntervalMode.OpenInterval, new Dictionary<double, int> { { 0.0, 0 } }), new RangeDictionary<double, int>(IntervalMode.OpenInterval, new Dictionary<double, int> { { 0.0, 0 } }));
            }
            else
            {
                var notePos = partTick + note.Pos;
                var offset = note.Duration - vibrato.Duration;
                var duration = tempo[notePos + note.Duration].TickToTime(notePos + note.Duration) - tempo[notePos + offset].TickToTime(notePos + offset);
                var depth = GetValue(vibrato.Depths, notePos, tempo);
                var rates = GetValue(vibrato.Rates, notePos, tempo);

                return new Vibrato(vibrato.Type, duration, depth, rates);
            }
        }

        RangeDictionary<double, int> GetValue(VprValue[] values, int pos, RangeDictionary<int, Tempo> tempo)
        {
            return values.ToRangeDictionary(
                p => tempo[pos + p.Pos].TickToTime(pos + p.Pos),
                p => p.Value,
                IntervalMode.OpenInterval
            );
        }

        RangeDictionary<double, double> GetControlChange(VprPart part, string name, double defaultValue, int partTick, RangeDictionary<int, Tempo> tempo)
        {
            var partStartTime = tempo[partTick].TickToTime(partTick);
            var result = Optional<VprController[]>.FromNull(part.Controllers)
                .SelectMany(cc => cc.Where(c => c.Name == name).SelectMany(c => c.Events))
                .TakeWhile(e => e.Pos < part.Duration)
                .ToRangeDictionary(e => tempo[partTick + e.Pos].TickToTime(partTick + e.Pos) - partStartTime, e => (double)e.Value, IntervalMode.OpenInterval);
            if (result.Count < 1 || !result.ContainsKey(0.0))
            {
                result.Add(0.0, defaultValue);
            }

            return result;
        }
    }
}
