using Intervallo.Audio;
using Intervallo.Audio.Player;
using Intervallo.Util;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Intervallo.Form
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        const int DoubleSize = sizeof(double);

        public MainWindow()
        {
            InitializeComponent();

            Timer.Tick += (sender, e) =>
            {
                var nowIndicatorIsVisible = WaveView.IndicatorIsVisible;
                WaveView.IndicatorPosition = Player.GetCurrentSample();
                if (nowIndicatorIsVisible && nowIndicatorIsVisible != WaveView.IndicatorIsVisible)
                {
                    WaveView.ScrollToIndicatorIfOutOfScreen();
                }
            };
            Timer.Stop();
        }

        DispatcherTimer Timer { get; } = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 16), IsEnabled = true };

        WavePlayer Player { get; set; }

        SeekPlayer SeekPlayer { get; set; }

        Wavefile Wavefile { get; set; }

        bool PlayingBeforeIndicatorMoving { get; set; }

        void PlayAudio()
        {
            Player.Play();
            Timer.Start();
        }

        void PauseAudio()
        {
            Player.Pause();
            Timer.Stop();
            WaveView.IndicatorPosition = Player.GetCurrentSample();
        }

        void WaveCanvas_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true) && (e.Data.GetData(DataFormats.FileDrop, true) as string[])?.Length == 1)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        void WaveCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                Player?.Stop();
                Player?.Dispose();

                Wavefile = Wavefile.Read((e.Data.GetData(DataFormats.FileDrop, true) as string[])[0]);
                WaveView.Wave = Wavefile.Data;
                WaveScaler.Wave = Wavefile.Data;
                WaveView.SampleRange = 0.To(Math.Min(30000, Wavefile.Data.Length));
                WaveView.SampleRate = Wavefile.Fs;

                SeekPlayer = new SeekPlayer(Wavefile.Fs);

                Player = new WavePlayer(Wavefile.Data, Wavefile.Fs);
                Player.EnableLoop = true;
                Player.PlaybackStopped += (s, ea) =>
                {
                    Player.SeekToStart();
                    WaveView.IndicatorPosition = Player.GetCurrentSample();
                };
            }
            catch (InvalidDataException ex)
            {

            }
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Player == null || e.Key != Key.Space)
            {
                return;
            }

            if (Player.PlaybackState == PlaybackState.Playing)
            {
                PauseAudio();
            }
            else
            {
                PlayAudio();
            }
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            Player?.Stop();
            Player?.Dispose();
            SeekPlayer?.Dispose();
        }

        void WaveView_IndicatorMoved(object sender, EventArgs e)
        {
            Player.SamplePosition = WaveView.IndicatorPosition;
            var bytePerSample = Wavefile.Bit / 8;
            var samples = new double[(int)(Wavefile.Fs * 0.05)];
            Buffer.BlockCopy(Wavefile.Data, WaveView.IndicatorPosition * DoubleSize, samples, 0, Math.Min(samples.Length, Wavefile.Data.Length - WaveView.IndicatorPosition) * DoubleSize);
            SeekPlayer.AddSample(samples);
        }

        void WaveView_IndicatorMoveFinish(object sender, EventArgs e)
        {
            if (Wavefile == null)
            {
                return;
            }

            SeekPlayer.Stop();
            if (PlayingBeforeIndicatorMoving)
            {
                PlayAudio();
            }
        }

        void WaveView_IndicatorMoveStart(object sender, EventArgs e)
        {
            if (Wavefile == null)
            {
                return;
            }

            PlayingBeforeIndicatorMoving = Wavefile != null && Player.PlaybackState == PlaybackState.Playing;
            PauseAudio();
            SeekPlayer.Play();
        }
    }
}
