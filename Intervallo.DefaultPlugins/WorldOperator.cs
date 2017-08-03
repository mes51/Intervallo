using Intervallo.DefaultPlugins.Properties;
using Intervallo.DefaultPlugins.WORLD;
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
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResource.WorldOperator_Description;

        public string PluginName => typeof(WorldOperator).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(WorldOperator).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public AnalyzedAudio Analyze(WaveData wave, double framePeriod, Action<double> notifyProgress)
        {
            // F0 estimate

            var harvest = new Harvest();
            harvest.FramePeriod = framePeriod;
            harvest.F0Floor = 40.0;
            
            var f0Length = harvest.GetSamplesForHarvest(wave.SampleRate, wave.Wave.Length, framePeriod);
            var f0 = new double[f0Length];
            var timeAxis = new double[f0Length];
            harvest.Estimate(wave.Wave, wave.SampleRate, timeAxis, f0);

            notifyProgress(1.0 / 3.0 * 100.0);

            // spectral envelope estimate

            var cheapTrick = new CheapTrick(wave.SampleRate);
            cheapTrick.F0Floor = 71.0;
            cheapTrick.FFTSize = cheapTrick.GetFFTSizeForCheapTrick(wave.SampleRate);

            var fftSize = cheapTrick.FFTSize;
            var spectrogram = Enumerable.Range(0, f0Length).Select((i) => new double[fftSize / 2 + 1]).ToArray();
            cheapTrick.Estimate(wave.Wave, wave.SampleRate, timeAxis, f0, spectrogram);

            notifyProgress(2.0 / 3.0 * 100.0);

            // aperiodicity estimate

            var d4c = new D4C();
            d4c.Threshold = 0.85;

            var aperiodicity = Enumerable.Range(0, f0Length).Select((i) => new double[fftSize / 2 + 1]).ToArray();
            d4c.Estimate(wave.Wave, wave.SampleRate, timeAxis, f0, f0Length, fftSize, aperiodicity);

            notifyProgress(100.0);

            return new WorldAnalyzedAudio(
                f0,
                framePeriod,
                wave.SampleRate,
                fftSize,
                timeAxis,
                spectrogram,
                aperiodicity
            );
        }

        public WaveData Synthesize(AnalyzedAudio analyzedAudio, Action<double> notifyProgress)
        {
            var waa = analyzedAudio as WorldAnalyzedAudio;

            var yLength = (int)((waa.FrameLength - 1) * waa.FramePeriod / 1000.0 * waa.Fs) + 1;
            var y = new double[yLength];
            var synthesis = new Synthesis();

            synthesis.Synthesize(waa.F0, waa.FrameLength, waa.Spectrogram, waa.Aperiodicity, waa.FFTSize, waa.FramePeriod, waa.Fs, y);

            return new WaveData(y, waa.Fs);
        }
    }

    [Serializable]
    public class WorldAnalyzedAudio : AnalyzedAudio
    {
        public int Fs { get; }

        public int FFTSize { get; }

        public double[] TimeAxis { get;  }

        public double[][] Spectrogram { get; }

        public double[][] Aperiodicity { get; }

        public WorldAnalyzedAudio(double[] f0, double framePeriod, int fs, int fftSize, double[] timeAxis, double[][] spectrogram, double[][] aperiodicity) : base(f0, framePeriod)
        {
            Fs = fs;
            FFTSize = fftSize;
            TimeAxis = timeAxis;
            Spectrogram = spectrogram;
            Aperiodicity = aperiodicity;
        }

        public override AnalyzedAudio Copy()
        {
            return new WorldAnalyzedAudio(
                F0.BlockClone(),
                FramePeriod,
                Fs,
                FFTSize,
                TimeAxis.BlockClone(),
                Spectrogram.BlockClone(),
                Aperiodicity.BlockClone()
            );
        }

        public override AnalyzedAudio ReplaceF0(double[] newF0)
        {
            if (newF0.Length != FrameLength)
            {
                throw new ArgumentException(nameof(newF0));
            }

            return new WorldAnalyzedAudio(
                newF0,
                FramePeriod,
                Fs,
                FFTSize,
                TimeAxis.BlockClone(),
                Spectrogram.BlockClone(),
                Aperiodicity.BlockClone()
            );
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

            return new WorldAnalyzedAudio(
                F0.BlockClone(begin, count),
                FramePeriod,
                Fs,
                FFTSize,
                TimeAxis.BlockClone(begin, count),
                Spectrogram.BlockClone(begin, count),
                Aperiodicity.BlockClone(begin, count)
            );
        }
    }
}
