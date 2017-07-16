using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Audio.Player
{
    public class SeekPlayer : IWavePlayer
    {
        public SeekPlayer(int fs)
        {
            Provider = new SampleBufferedWaveProvider(fs);

            using (var mmDeviceEnumerator = new MMDeviceEnumerator())
            {
                Player = new WasapiOut(mmDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia), AudioClientShareMode.Shared, false, 50);
                Player.Init(Provider);
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

        WasapiOut Player { get; }

        SampleBufferedWaveProvider Provider { get; }

        public void Init(IWaveProvider waveProvider)
        {
            throw new InvalidOperationException("already initialized");
        }

        public void Pause()
        {
            Player.Pause();
        }

        public void Play()
        {
            Player.Play();
        }

        public void Stop()
        {
            Player.Stop();
            Provider.ClearBuffer();
        }

        public void AddSample(double[] sample)
        {
            Provider.ClearBuffer();
            Provider.AddSamples(sample);
        }

        public void Dispose()
        {
            Player.Dispose();
        }
    }
}
