using Intervallo.Audio;
using Intervallo.Audio.Player;
using Intervallo.Util;
using NAudio.Wave;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Intervallo.Plugin;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Intervallo.Cache;
using System.Threading.Tasks;
using Intervallo.Properties;

namespace Intervallo.Form
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        const int DoubleSize = sizeof(double);
        const string PluginDirectory = "Plugins";

        public MainWindow()
        {
            InitializeComponent();

            Timer.Tick += (sender, e) =>
            {
                var nowIndicatorIsVisible = MainView.IndicatorIsVisible;
                MainView.IndicatorPosition = Player.GetCurrentSample();
                if (nowIndicatorIsVisible && nowIndicatorIsVisible != MainView.IndicatorIsVisible)
                {
                    MainView.ScrollToIndicatorIfOutOfScreen();
                }
            };
            Timer.Stop();

            LoadPlugin();
            CacheFile.CreateCacheDirectory();
        }

        DispatcherTimer Timer { get; } = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 16), IsEnabled = true };

        WavePlayer Player { get; set; }

        SeekPlayer SeekPlayer { get; set; }

        Wavefile WaveData { get; set; }

        bool PlayingBeforeIndicatorMoving { get; set; }

        [ImportMany]
        List<IAudioOperator> AudioOperatorPlugins { get; set; }

        [ImportMany]
        List<IScaleLoader> ScaleLoaderPlugins { get; set; }

        void LoadPlugin()
        {
            try
            {
                using (var catalog = new DirectoryCatalog(PluginDirectory))
                using (var container = new CompositionContainer(catalog))
                {
                    container.ComposeParts(this);
                }
            }
            catch(Exception e) { }
        }

        void PlayAudio()
        {
            Player.Play();
            Timer.Start();
        }

        void PauseAudio()
        {
            Player.Pause();
            Timer.Stop();
            MainView.IndicatorPosition = Player.GetCurrentSample();
        }

        async void LoadWave(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    WaveData = Wavefile.Read(filePath);

                    var waveLineCache = WaveLineCache.CreateCache(WaveData.Data, WaveData.Fs, WaveData.Hash);

                    Dispatcher.Invoke(() =>
                    {
                        MainView.Wave = waveLineCache;
                        MainView.SampleRange = 0.To(30000);

                        SeekPlayer = new SeekPlayer(WaveData.Fs);

                        Player = new WavePlayer(WaveData.Data, WaveData.Fs);
                        Player.EnableLoop = true;
                        Player.PlaybackStopped += (s, ea) =>
                        {
                            Player.SeekToStart();
                            MainView.IndicatorPosition = Player.GetCurrentSample();
                        };

                        MainView.Progress = 25.0;
                        MainView.MessageText = TextResources.ProgressMessageAnalyzingWave;
                    });

                    var aaCache = CacheFile.FindCache<AnalyzedAudioCache>(WaveData.Hash + AudioOperatorPlugins[0].GetType().FullName)
                        .GetOrElse(() =>
                        {
                            var aa = AudioOperatorPlugins[0].Analyze(new Plugin.WaveData(WaveData.Data, WaveData.Fs), 5.0, (p) =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MainView.Progress = p * 0.75 + 25.0;
                                });
                            });
                            var result = new AnalyzedAudioCache(AudioOperatorPlugins[0].GetType(), aa, WaveData.Hash);
                            CacheFile.SaveCache(result, WaveData.Hash + AudioOperatorPlugins[0].GetType().FullName);
                            return result;
                        });

                    Dispatcher.Invoke(() =>
                    {
                        MainView.Lock = false;
                    });
                }
                catch (InvalidDataException ex)
                {

                }
                catch (Exception e)
                {

                }
            });
        }

        void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            var filePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true) && Path.GetExtension(filePaths?[0] ?? "") == ".wav")
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        void Window_Drop(object sender, DragEventArgs e)
        {
            Player?.Stop();
            Player?.Dispose();

            MainView.MessageText = TextResources.ProgressMessageLoadWave;
            MainView.Progress = 0.0;
            MainView.Lock = true;
            LoadWave((e.Data.GetData(DataFormats.FileDrop, true) as string[])[0]);
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

        void MainView_IndicatorMoved(object sender, EventArgs e)
        {
            Player.SamplePosition = MainView.IndicatorPosition;
            var samples = new double[(int)(WaveData.Fs * 0.05)];
            Buffer.BlockCopy(WaveData.Data, MainView.IndicatorPosition * DoubleSize, samples, 0, Math.Min(samples.Length, WaveData.Data.Length - MainView.IndicatorPosition) * DoubleSize);
            SeekPlayer.AddSample(samples);
        }

        void MainView_IndicatorMoveFinish(object sender, EventArgs e)
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

        void MainView_IndicatorMoveStart(object sender, EventArgs e)
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
