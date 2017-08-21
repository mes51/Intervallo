using Intervallo.DefaultPlugins.Properties;
using Intervallo.DefaultPlugins.WORLD;
using Intervallo.InternalUtil;
using Intervallo.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.DefaultPlugins
{
    [Export(typeof(IAudioOperator))]
    public class WorldOperator : IAudioOperator
    {
        const int FrameSizeRate = 2;
        /// <summary>
        /// include FrameSizeRate.
        /// real frames = CombinableFrameGap * FrameSizeRate
        /// </summary>
        const double CombinableFrameGap = 10.0;

        static readonly object LockObject = new object();

        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResources.WorldOperator_Description;

        public string PluginName => typeof(WorldOperator).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public AnalyzedAudio Analyze(WaveData wave, double framePeriod, Action<double> notifyProgress)
        {
            var frameSize = wave.SampleRate * framePeriod * 0.001 * FrameSizeRate;
            var frameCount = (int)Math.Ceiling(wave.Wave.Length / frameSize);
            var silentFrames = wave.Wave.SplitByIndexes(
                Enumerable.Range(0, frameCount).Select((i) => (int)Math.Ceiling(i * frameSize)),
                (frame, start, i) => new Frame(start, i, frame)
            ).Where((x) => x.Silent).ToArray();

            var elements = new List<AnalyzedElement>();
            if (silentFrames.Any())
            {
                if (silentFrames.First().Index != 0)
                {
                    silentFrames = new Frame(0, 0, (int)Math.Ceiling(frameSize), false).PushTo(silentFrames).ToArray();
                }

                var capFrames = silentFrames.Zip3(
                    new Frame(silentFrames[0], silentFrames[0].Index - 1).PushTo(silentFrames),
                    silentFrames.Skip(1).Append(new Frame(silentFrames.Last(), silentFrames.Last().Index + 1)),
                    (f, s, t) => new { Target = f, Prev = s, Next = t }
                )
                .Where((x) => (x.Prev.Index + 1 == x.Target.Index && x.Target.Index + 1 != x.Next.Index) || (x.Prev.Index + 1 != x.Target.Index && x.Target.Index + 1 == x.Next.Index))
                .Select((x) => x.Target)
                .ToArray()
                .AsEnumerable();
                

                foreach (var cap in capFrames.Grouped(2))
                {
                    var first = cap.First();
                    if (cap.Count() < 2)
                    {
                        elements.Add(
                            new AnalyzedElement(
                                wave.Wave.Skip(first.Position).ToArray(),
                                first.Position,
                                first.Index * FrameSizeRate,
                                wave.SampleRate,
                                framePeriod,
                                first.Silent
                            )
                        );
                    }
                    else
                    {
                        elements.Add(
                            new AnalyzedElement(
                                wave.Wave.Skip(first.Position).Take(cap.Last().Position - first.Position + cap.Last().FrameSize).ToArray(),
                                first.Position,
                                first.Index * FrameSizeRate,
                                wave.SampleRate,
                                framePeriod,
                                first.Silent
                            )
                        );
                    }
                }
            }
            else
            {
                elements.Add(new AnalyzedElement(wave.Wave, 0, 0, wave.SampleRate, framePeriod, false));
            }

            notifyProgress(1 / (double)(elements.Count + 1) * 100.0);

            var progress = 1;
            var skipElement = new List<AnalyzedElement>();
            var combineElement = new List<CombineElement>();
            Parallel.For(0, elements.Count, (i) =>
            {
                var target = elements[i];
                try
                {
                    target.Analyze();

                    lock (LockObject)
                    {
                        progress++;
                        notifyProgress(progress / (double)(elements.Count + 1) * 100.0);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    var side = new List<AnalyzedElement>();
                    if (i > 0 && (elements[i - 1].SamplePosition + elements[i - 1].Wave.Length - target.SamplePosition) / frameSize <= CombinableFrameGap)
                    {
                        side.Add(elements[i - 1]);
                    }
                    if (i < elements.Count - 1 && (elements[i + 1].SamplePosition - target.SamplePosition - target.Wave.Length) / frameSize <= CombinableFrameGap)
                    {
                        side.Add(elements[i + 1]);
                    }
                    if (side.Count > 0)
                    {
                        combineElement.Add(new CombineElement(new List<AnalyzedElement>() { target }, side));
                    }
                    else
                    {
                        skipElement.Add(target);
                    }
                }
            });

            elements.RemoveAll(skipElement.Contains);
            while (true)
            {
                var concat = combineElement.Zip(combineElement.Skip(1), (t, n) => Optional<CombineElement>.Iif(() => t.Concat(n), t.CanConcat(n)));

                if (concat.All((c) => c.IsEmpty))
                {
                    break;
                }
                else
                {
                    combineElement = concat.SelectMany((c) => c).ToList();
                }
            }

            double totalCount = elements.Count + combineElement.Count + 1.0;
            progress = elements.Count + 1;
            notifyProgress(progress / totalCount * 100.0);
            Parallel.For(0, combineElement.Count, (i) =>
            {
                try
                {
                    var element = combineElement[i].Combine(wave.Wave);
                    element.Analyze();

                    lock (LockObject)
                    {
                        elements.RemoveAll(combineElement[i].Targets.Contains);
                        elements.RemoveAll(combineElement[i].SideElements.Contains);
                        elements.Add(element);
                        progress++;
                        notifyProgress(progress / totalCount * 100.0);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    lock (LockObject)
                    {
                        elements.RemoveAll(combineElement[i].Targets.Contains);
                        progress++;
                        notifyProgress(progress / totalCount * 100.0);
                    }
                }
            });

            elements.Sort();

            var f0 = new double[(int)Math.Ceiling((wave.Wave.Length / frameSize) * FrameSizeRate)];
            foreach (var element in elements)
            {
                element.F0.BlockCopy(f0, element.FramePosition);
            }

            return new WorldAnalyzedAudio(f0, framePeriod, wave.SampleRate, wave.Wave.Length, 0, elements.ToArray());
        }

        public WaveData Synthesize(AnalyzedAudio analyzedAudio, Action<double> notifyProgress)
        {
            var waa = analyzedAudio as WorldAnalyzedAudio;
            var samplePosition = (int)Math.Ceiling(waa.BeginFrame * waa.FrameSize);
            var result = new double[waa.SampleCount + 1];
            var fadeRad = Math.PI * 2.0 / Math.Floor(waa.FrameSize);
            var beginFade = Enumerable.Range(0, (int)waa.FrameSize).Select((i) => (1.0 + Math.Tanh(i * fadeRad - Math.PI)) * 0.5).ToArray();
            var endFade = beginFade.Reverse().ToArray();

            var progress = 0;
            Parallel.For(0, waa.Elements.Length, (i) =>
            {
                var e = waa.Elements[i];
                if (e.FramePosition < waa.BeginFrame || e.FramePosition > waa.BeginFrame + waa.FrameLength)
                {
                    return;
                }

                var w = e.Synthesize();

                if (e.SilentStart)
                {
                    for (var n = 0; n < beginFade.Length; n++)
                    {
                        w[n] *= beginFade[n];
                    }
                }
                for (var n = 0; n < endFade.Length; n++)
                {
                    w[w.Length - endFade.Length + n] *= endFade[n];
                }

                lock (LockObject)
                {
                    var srcOffset = Math.Max(samplePosition - e.SamplePosition, 0);
                    var dstOffset = Math.Max(e.SamplePosition - samplePosition, 0);
                    w.BlockCopy(srcOffset, result, dstOffset, Math.Min(w.Length - srcOffset, result.Length - dstOffset));
                    progress++;
                    notifyProgress(progress / (double)waa.Elements.Length * 100.0);
                }
            });

            return new WaveData(
                result.Skip((int)(waa.BeginFrame * waa.FrameSize))
                    .Take((int)Math.Ceiling((waa.BeginFrame + waa.FrameLength) * waa.FrameSize))
                    .ToArray(),
                waa.SampleRate
            );
        }

        class CombineElement
        {
            public CombineElement(List<AnalyzedElement> targets, List<AnalyzedElement> sideElements)
            {
                Targets = targets;
                SideElements = sideElements;
            }

            public List<AnalyzedElement> Targets { get; }

            public List<AnalyzedElement> SideElements { get; }

            public bool CanConcat(CombineElement element)
            {
                return element.Targets.Any(SideElements.Contains) || element.SideElements.Any(Targets.Concat(SideElements).Contains);
            }

            public CombineElement Concat(CombineElement element)
            {
                return new CombineElement(Targets.Concat(element.Targets).ToList(), SideElements.Union(element.SideElements).ToList());
            }

            public AnalyzedElement Combine(double[] wave)
            {
                var allElements = SideElements.Concat(Targets).OrderBy((e) => e.SamplePosition);
                var first = allElements.First();
                var last = allElements.Last();

                return new AnalyzedElement(
                    wave.Skip(first.SamplePosition).Take(last.SamplePosition + last.Wave.Length - first.SamplePosition).ToArray(),
                    first.SamplePosition,
                    first.FramePosition,
                    first.SampleRate,
                    first.FramePeriod,
                    first.SilentStart
                );
            }
        }

        class Frame
        {
            public Frame(int position, int index, IEnumerable<double> frame)
                : this(position, index, frame.Count(), 20.0 * Math.Log10(frame.Max()) < -59.0) { }

            public Frame(Frame frame, int newIndex)
                : this(frame.Position, newIndex, frame.FrameSize, frame.Silent) { }

            public Frame(int position, int index, int frameSize, bool silent)
            {
                Position = position;
                Index = index;
                FrameSize = frameSize;
                Silent = silent;
            }

            public int Position { get; }

            public int Index { get; }

            public int FrameSize { get; }

            public bool Silent { get; }
        }
    }

    [Serializable]
    public class AnalyzedElement : IComparable<AnalyzedElement>
    {
        /// <summary>
        /// do not access direct
        /// </summary>
        [NonSerialized]
        private double[] nonSerializedWave = null;

        public double[] Wave
        {
            get { return nonSerializedWave; }
            private set { nonSerializedWave = value; }
        }

        public int SamplePosition { get; }

        public int FramePosition { get; }

        public int SampleRate { get; }

        public double FramePeriod { get; }

        public bool SilentStart { get; }

        public int FFTSize { get; set; }

        public double[] F0 { get; set; }

        public double[] TimeAxis { get; set; }

        public double[][] Spectrogram { get; set; }

        public double[][] Aperiodicity { get; set; }

        public AnalyzedElement(double[] wave, int samplePosition, int framePosition, int sampleRate, double framePeriod, bool silentStart)
        {
            Wave = wave;
            SamplePosition = samplePosition;
            FramePosition = framePosition;
            SampleRate = sampleRate;
            FramePeriod = framePeriod;
            SilentStart = silentStart;
        }

        public void Analyze()
        {
            // F0 estimate

            var harvest = new Harvest();
            harvest.FramePeriod = FramePeriod;
            harvest.F0Floor = 40.0;
            var f0Length = harvest.GetSamplesForHarvest(SampleRate, Wave.Length, FramePeriod);
            F0 = new double[f0Length];
            TimeAxis = new double[f0Length];
            harvest.Estimate(Wave, SampleRate, TimeAxis, F0);

            // spectral envelope estimate

            var cheapTrick = new CheapTrick(SampleRate);
            cheapTrick.F0Floor = 71.0;
            cheapTrick.FFTSize = cheapTrick.GetFFTSizeForCheapTrick(SampleRate);
            FFTSize = cheapTrick.FFTSize;
            Spectrogram = Enumerable.Range(0, f0Length).Select((i) => new double[FFTSize / 2 + 1]).ToArray();
            cheapTrick.Estimate(Wave, SampleRate, TimeAxis, F0, Spectrogram);

            // aperiodicity estimate

            var d4c = new D4C();
            d4c.Threshold = 0.85;
            Aperiodicity = Enumerable.Range(0, f0Length).Select((i) => new double[FFTSize / 2 + 1]).ToArray();
            d4c.Estimate(Wave, SampleRate, TimeAxis, F0, f0Length, FFTSize, Aperiodicity);
        }

        public double[] Synthesize()
        {
            var yLength = (int)((F0.Length - 1) * FramePeriod / 1000.0 * SampleRate) + 1;
            var y = new double[yLength];
            var synthesis = new Synthesis();

            synthesis.Synthesize(F0, F0.Length, Spectrogram, Aperiodicity, FFTSize, FramePeriod, SampleRate, y);

            return y;
        }

        public AnalyzedElement ReplaceF0(double[] newF0)
        {
            var result = new AnalyzedElement(Wave, SamplePosition, FramePosition, SampleRate, FramePeriod, SilentStart);
            result.FFTSize = FFTSize;
            result.F0 = newF0;
            result.TimeAxis = TimeAxis;
            result.Spectrogram = Spectrogram;
            result.Aperiodicity = Aperiodicity;

            return result;
        }

        public int CompareTo(AnalyzedElement other)
        {
            return SamplePosition - other.SamplePosition;
        }
    }

    [Serializable]
    public class WorldAnalyzedAudio : AnalyzedAudio
    {
        public int SampleRate { get; }

        public int SampleCount { get; }

        public int BeginFrame { get; }

        public AnalyzedElement[] Elements { get; }

        public double FrameSize => SampleRate * FramePeriod * 0.001;

        public WorldAnalyzedAudio(double[] f0, double framePeriod, int sampleRate, int sampleCount, int beginFrame, AnalyzedElement[] elements) : base(f0, framePeriod)
        {
            SampleRate = sampleRate;
            SampleCount = sampleCount;
            BeginFrame = beginFrame;
            Elements = elements;
        }

        public override AnalyzedAudio Copy()
        {
            return new WorldAnalyzedAudio(F0.BlockClone(), FramePeriod, SampleRate, SampleCount, BeginFrame, Elements);
        }

        public override AnalyzedAudio ReplaceF0(double[] newF0)
        {
            if (newF0.Length != FrameLength)
            {
                throw new ArgumentException(nameof(newF0));
            }

            var newElements = Elements.Select((e) => e.ReplaceF0(newF0.Skip(e.FramePosition).Take(e.F0.Length).ToArray())).ToArray();

            return new WorldAnalyzedAudio(newF0, FramePeriod, SampleRate, SampleCount, BeginFrame, newElements);
        }

        public override AnalyzedAudio Slice(int begin, int count)
        {
            if (count < 1)
            {
                throw new ArgumentException(nameof(count));
            }
            if (begin < 0 || begin + count >= FrameLength)
            {
                throw new ArgumentOutOfRangeException();
            }

            var list = Elements.ToList();
            var end = begin + count;
            var beginElementIndex = Math.Max(list.FindIndex((e) => e.FramePosition > begin) - 1, 0);
            var elementCount = list.FindLastIndex((e) => e.FramePosition + e.F0.Length > end) + 1;
            var newElements = Elements.Skip(beginElementIndex).Take(elementCount).ToArray();
            return new WorldAnalyzedAudio(
                F0.BlockClone(begin, count),
                FramePeriod,
                SampleRate,
                (int)Math.Ceiling((newElements.Last().FramePosition + newElements.Last().F0.Length) * FrameSize) - (int)Math.Ceiling(newElements[0].FramePosition * FrameSize),
                begin,
                newElements
            );
        }
    }
}
