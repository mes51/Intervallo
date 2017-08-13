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
            Track[] tracks = null;
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    tracks = LoadFromVsq4(fs, framePeriod);
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
                        f0 = selectWindow.SelectedTrack.ToF0(maxFrameLength, framePeriod);
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
                return tracks[0].ToF0(maxFrameLength, framePeriod);
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
                            .Select((n) =>
                            {
                                var vibDepth = n.nStyle.seq?
                                    .FirstOrDefault((s) => s.id == "vibDep")?.cc
                                    .ToRangeDictionary((c) => c.p, (c) => c.v, IntervalMode.OpenInterval)
                                    ?? new RangeDictionary<int, int>(IntervalMode.OpenInterval, new Dictionary<int, int>() { [0] = 0 });
                                var vibRate = n.nStyle.seq?
                                    .FirstOrDefault((s) => s.id == "vibRate")?.cc
                                    .ToRangeDictionary((c) => c.p, (c) => c.v, IntervalMode.OpenInterval)
                                    ?? new RangeDictionary<int, int>(IntervalMode.OpenInterval, new Dictionary<int, int>() { [0] = 0 });
                                var vibLength = n.nStyle.v.FirstOrDefault((st) => st.id == "vibLen")?.Value ?? 0;
                                var vibType = n.nStyle.v.FirstOrDefault((st) => st.id == "vibType")?.Value ?? 0;

                                return new Note(n.t, n.dur, n.n, vibLength, vibType, vibDepth, vibRate);
                            })
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

                return new Track(t.name, tempo, parts);
            }).ToArray();
        }
    }
}
