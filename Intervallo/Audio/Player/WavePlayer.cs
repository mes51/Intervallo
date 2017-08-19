using Intervallo.Config;
using Intervallo.Util;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    public class WavePlayer : IWavePlayer
    {
        public WavePlayer(double[] wave, int fs)
        {
            Stream = new LoopableWaveStream(wave, fs);
            SampleRate = fs;

            using (var mmDeviceEnumerator = new MMDeviceEnumerator())
            {
                Player = new WasapiOut(mmDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia), AudioClientShareMode.Shared, false, ApplicationSettings.Setting.Audio.PreviewLatency);
                Player.Init(Stream);
            }
        }

        public byte[] Data { get; }

        public int SampleRate { get; }

        public IntRange LoopRange
        {
            get
            {
                return Stream.LoopRange;
            }
            set
            {
                Stream.LoopRange = value;
            }
        }

        public bool EnableLoop
        {
            get
            {
                return Stream.EnableLoop;
            }
            set
            {
                Stream.EnableLoop = value;
            }
        }

        public int SamplePosition
        {
            get
            {
                return Stream.SamplePosition;
            }
            set
            {
                Stream.SamplePosition = value;
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                return Player.PlaybackState;
            }
        }

        public float Volume
        {
            get
            {
                return Player.Volume;
            }
            set
            {
                Player.Volume = value;
            }
        }

        public event EventHandler<StoppedEventArgs> PlaybackStopped
        {
            add
            {
                Player.PlaybackStopped += value;
            }
            remove
            {
                Player.PlaybackStopped -= value;
            }
        }

        LoopableWaveStream Stream { get; }

        WasapiOut Player { get; }

        KeyValuePair<long, long> PositionLog { get; set; }

        public int GetCurrentSample()
        {
            if (Player.PlaybackState == PlaybackState.Stopped)
            {
                return Stream.SamplePosition;
            }

            var playedCount = (int)((Player.GetPosition() - PositionLog.Value) / (double)Player.OutputWaveFormat.AverageBytesPerSecond * SampleRate);
            var history = Stream.GetHistory(PositionLog.Key + playedCount);
            if (history == null)
            {
                return Stream.SamplePosition;
            }
            var progressedCount = playedCount - (history.BeginTotalReadSamples - PositionLog.Key);
            return (int)(progressedCount + history.BeginReadPosition);
        }

        public void Play()
        {
            PositionLog = new KeyValuePair<long, long>(Stream.TotalReadSamples, Player.GetPosition());
            Player.Play();
        }

        public void Stop()
        {
            Player.Stop();
        }

        public void Pause()
        {
            Player.Pause();
        }

        public void SeekToStart()
        {
            Stream.Position = 0;
        }

        public void Init(IWaveProvider waveProvider)
        {
            throw new InvalidOperationException("already initialized");
        }

        public void Dispose()
        {
            Player.Dispose();
            Stream.Dispose();
        }
    }
}
