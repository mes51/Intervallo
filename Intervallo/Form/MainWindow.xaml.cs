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

        WaveData WaveData { get; set; }

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

                var fileName = (e.Data.GetData(DataFormats.FileDrop, true) as string[])[0];
                var wavefile = Wavefile.Read(fileName);

                WaveData = new WaveData(fileName, wavefile.Data, wavefile.Fs);
                WaveView.Wave = WaveData;
                WaveScaler.Wave = WaveData;
                WaveView.SampleRange = 0.To(Math.Min(30000, WaveData.Wave.Length));

                SeekPlayer = new SeekPlayer(WaveData.SampleRate);

                Player = new WavePlayer(WaveData.Wave, WaveData.SampleRate);
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
            var samples = new double[(int)(WaveData.SampleRate * 0.05)];
            Buffer.BlockCopy(WaveData.Wave, WaveView.IndicatorPosition * DoubleSize, samples, 0, Math.Min(samples.Length, WaveData.Wave.Length - WaveView.IndicatorPosition) * DoubleSize);
            SeekPlayer.AddSample(samples);
        }

        void WaveView_IndicatorMoveFinish(object sender, EventArgs e)
        {
            if (WaveData == null)
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
            if (WaveData == null)
            {
                return;
            }

            PlayingBeforeIndicatorMoving = WaveData != null && Player.PlaybackState == PlaybackState.Playing;
            PauseAudio();
            SeekPlayer.Play();
        }
    }
}
