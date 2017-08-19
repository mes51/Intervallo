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
using Intervallo.Model;
using Microsoft.Win32;
using Intervallo.Command;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Intervallo.Config;

namespace Intervallo.Form
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        const int DoubleSize = sizeof(double);
        const string PluginDirectory = "Plugins";
        static readonly Regex LoaderExtentionRegex = new Regex(@"^\*?\.?", RegexOptions.Compiled);

        public CommandBase OpenCommand { get; }
        public CommandBase ExportCommand { get; }
        public CommandBase ExitCommand { get; }
        public CommandBase LoadScaleCommand { get; }
        public CommandBase LoadScaleFromWaveCommand { get; }
        public CommandBase PreviewCommand { get; }
        public CommandBase ClearCacheCommand { get; }
        public CommandBase OptionCommand { get; }
        public CommandBase AboutCommand { get; }

        public MainWindow()
        {
            OpenCommand = new OpenCommand(this);
            ExportCommand = new ExportCommand(this);
            ExitCommand = new ActionCommand(this, () => Close(), true);
            LoadScaleCommand = new LoadScaleCommand(this, true);
            LoadScaleFromWaveCommand = new LoadScaleCommand(this, false);
            PreviewCommand = new PreviewCommand(this);
            ClearCacheCommand = new ActionCommand(this, () => CacheFile.ClearChaceFile(), true);
            OptionCommand = new ActionCommand(this, () => {
                var window = new OptionWindow();
                window.Owner = this;
                window.ShowDialog();
            }, true);
            AboutCommand = new ActionCommand(this, () => {
                var window = new AboutWindow();
                window.Owner = this;
                window.ShowDialog();
            });

            InitializeComponent();
            Top = ApplicationSettings.Setting.General.Position.Y;
            Left = ApplicationSettings.Setting.General.Position.X;
            Width = ApplicationSettings.Setting.General.Size.Width;
            Height = ApplicationSettings.Setting.General.Size.Height;
            WindowState = ApplicationSettings.Setting.General.State;

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

                ScaleLoaderPlugins.Sort((a, b) => b.PluginName.CompareTo(a.PluginName));
                foreach (var loader in ScaleLoaderPlugins)
                {
                    var item = new MenuItem();
                    item.Header = loader.PluginName + "...";
                    item.Command = LoadScaleCommand;
                    item.CommandParameter = loader;
                    item.CommandTarget = this;
                    LoadScaleMenu.Items.Insert(0, item);
                }
                ScaleLoaderPlugins.Reverse();
            }
            catch (Exception e)
            {
                MessageBox.ShowWarning(LangResources.Error_RaisePluginLoadError, exception: e);
            }

            if (AudioOperatorPlugins.Count < 1)
            {
                MessageBox.ShowError(LangResources.Fatal_CannotLoadAudioOperator, LangResources.MessageBoxTitle_CannotLoadAudioOperator);
                Close();
            }
            else if (ScaleLoaderPlugins.Count < 1)
            {
                MessageBox.ShowWarning(LangResources.Error_CannotLoadScaleLoader, LangResources.MessageBoxTitle_CannotLoadScaleLoader);
            }
        }

        #region TODO: Move to ModelView

        public bool Lock
        {
            get
            {
                return MainView.Lock;
            }
            private set
            {
                if (MainView.Lock != value)
                {
                    MainView.Lock = value;
                    OpenCommand.OnCanExecuteChanged();
                    ExportCommand.OnCanExecuteChanged();
                    ExitCommand.OnCanExecuteChanged();
                    PreviewCommand.OnCanExecuteChanged();
                    LoadScaleCommand.OnCanExecuteChanged();
                    LoadScaleFromWaveCommand.OnCanExecuteChanged();
                }
            }
        }

        Wavefile WaveData { get; set; }

        AnalyzedAudioCache AnalyzedAudio { get; set; }

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
                        MainView.MessageText = LangResources.ProgressMessage_AnalyzingWave;
                    });

                    AnalyzedAudio = CacheFile.FindCache<AnalyzedAudioCache>(WaveData.Hash + AudioOperatorPlugins[0].GetType().FullName)
                        .GetOrElse(() =>
                        {
                            var aa = AudioOperatorPlugins[0].Analyze(new WaveData(WaveData.Data, WaveData.Fs), ApplicationSettings.Setting.PitchOperation.FramePeriod, (p) =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MainView.Progress = p * 0.75 + 25.0;
                                });
                            });
                            var result = new AnalyzedAudioCache(AudioOperatorPlugins[0].GetType(), aa, WaveData.Data.Length, WaveData.Fs, WaveData.Hash);
                            CacheFile.SaveCache(result, WaveData.Hash + AudioOperatorPlugins[0].GetType().FullName);
                            return result;
                        });

                    Dispatcher.Invoke(() =>
                    {
                        MainView.AudioScale = new AudioScaleModel(AnalyzedAudio.AnalyzedAudio.F0, AnalyzedAudio.AnalyzedAudio.FramePeriod, AnalyzedAudio.SampleCount, AnalyzedAudio.SampleRate);
                        MainView.EditableAudioScale = new AudioScaleModel(AnalyzedAudio.AnalyzedAudio.F0, AnalyzedAudio.AnalyzedAudio.FramePeriod, AnalyzedAudio.SampleCount, AnalyzedAudio.SampleRate);
                        Lock = false;
                    });
                }
                catch (InvalidDataException)
                {
                    MessageBox.ShowWarning(LangResources.Error_UnsupportedWaveFile, LangResources.MessageBoxTitle_CannotLoadWaveFile);

                    Dispatcher.Invoke(() =>
                    {
                        Player?.Stop();
                        Player?.Dispose();
                        MainView.Wave = null;
                        MainView.AudioScale = null;
                        Lock = false;
                    });
                }
                catch (Exception e)
                {
                    MessageBox.ShowWarning(LangResources.Error_CannodLoadWaveFile, exception: e);

                    Dispatcher.Invoke(() =>
                    {
                        Player?.Stop();
                        Player?.Dispose();
                        MainView.Wave = null;
                        MainView.AudioScale = null;
                        Lock = false;
                    });
                }
            });
        }

        async void ExportWave(string filePath, double[] newF0)
        {
            await Task.Run(() =>
            {
                var edited = AnalyzedAudio.AnalyzedAudio.ReplaceF0(newF0);
                var synthesizedAudio = AudioOperatorPlugins[0].Synthesize(edited, (p) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MainView.Progress = p;
                    });
                });

                var waveFile = new Wavefile(synthesizedAudio.SampleRate, WaveData.Bit, synthesizedAudio.Wave, "");
                waveFile.Write(filePath);

                Dispatcher.Invoke(() =>
                {
                    Lock = false;
                });
            });
        }

        void OpenFile(string filePath)
        {
            Player?.Stop();
            Player?.Dispose();

            MainView.Wave = null;
            MainView.AudioScale = null;
            MainView.MessageText = LangResources.ProgressMessage_LoadWave;
            MainView.Progress = 0.0;
            Lock = true;
            LoadWave(filePath);
        }

        void ApplyScale(double[] newScale)
        {
            var newF0 = new double[AnalyzedAudio.AnalyzedAudio.FrameLength];
            Buffer.BlockCopy(AnalyzedAudio.AnalyzedAudio.F0, 0, newF0, 0, newF0.Length * sizeof(double));

            for (var i = Math.Min(newScale.Length, newF0.Length) - 1; i > -1; i--)
            {
                if (newF0[i] <= 0.0 || newScale[i] <= 0.0)
                {
                    continue;
                }
                newF0[i] = newScale[i];
            }
            MainView.EditableAudioScale = new AudioScaleModel(newF0, AnalyzedAudio.AnalyzedAudio.FramePeriod, AnalyzedAudio.SampleCount, AnalyzedAudio.SampleRate);
        }

        async void LoadScale(IScaleLoader loader, string filePath)
        {
            MainView.Progress = 0.0;
            Lock = true;
            MainView.MessageText = LangResources.ProgressMessage_LoadScale;

            await Task.Run(() =>
            {
                try
                {
                    var loadedScale = loader.Load(filePath, AnalyzedAudio.AnalyzedAudio.FramePeriod, AnalyzedAudio.AnalyzedAudio.FrameLength);

                    Dispatcher.Invoke(() =>
                    {
                        MainView.Progress = 100.0;
                        ApplyScale(loadedScale);
                        Lock = false;
                    });
                }
                catch (Exception e)
                {
                    MessageBox.ShowWarning(LangResources.Error_RaiseLoadScaleError + e.Message, exception: e);

                    Dispatcher.Invoke(() =>
                    {
                        Lock = false;
                    });
                }
            });
        }

        async void LoadScaleFromWave(string filePath)
        {
            MainView.Progress = 0.0;
            Lock = true;
            MainView.MessageText = LangResources.ProgressMessage_LoadScale;

            await Task.Run(() =>
            {
                try
                {
                    var wave = Wavefile.Read(filePath);
                    var aac = CacheFile.FindCache<AnalyzedAudioCache>(wave.Hash + AudioOperatorPlugins[0].GetType().FullName)
                        .GetOrElse(() =>
                        {
                            var aa = AudioOperatorPlugins[0].Analyze(new Plugin.WaveData(wave.Data, wave.Fs), ApplicationSettings.Setting.PitchOperation.FramePeriod, (p) =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MainView.Progress = p * 0.75 + 25.0;
                                });
                            });
                            var result = new AnalyzedAudioCache(AudioOperatorPlugins[0].GetType(), aa, wave.Data.Length, wave.Fs, wave.Hash);
                            CacheFile.SaveCache(result, wave.Hash + AudioOperatorPlugins[0].GetType().FullName);
                            return result;
                        });

                    Dispatcher.Invoke(() =>
                    {
                        MainView.Progress = 1000.0;
                        ApplyScale(aac.AnalyzedAudio.F0);
                        Lock = false;
                    });
                }
                catch (InvalidDataException)
                {
                    MessageBox.ShowWarning(LangResources.Error_UnsupportedWaveFile, LangResources.MessageBoxTitle_CannotLoadWaveFile);

                    Dispatcher.Invoke(() =>
                    {
                        Lock = false;
                    });
                }
                catch (Exception e)
                {
                    MessageBox.ShowWarning(LangResources.Error_CannodLoadWaveFile, exception: e);

                    Dispatcher.Invoke(() =>
                    {
                        Lock = false;
                    });
                }
            });
        }

        #endregion

        #region Command

        public void ExecOpen()
        {
            var open = new OpenFileDialog();
            open.Filter = "Wave PCM(*.wav)|*.wav";
            if (open.ShowDialog() ?? false)
            {
                OpenFile(open.FileName);
            }
        }

        public void ExecExportWave()
        {
            var save = new SaveFileDialog();
            save.Filter = "Wave PCM(*.wav)|*.wav";
            if (save.ShowDialog() ?? false)
            {
                MainView.Progress = 0.0;
                Lock = true;
                MainView.MessageText = LangResources.ProgressMessage_ExportWave;
                ExportWave(save.FileName, MainView.EditableAudioScale.F0);
            }
        }

        public void ExecPlayOrStop()
        {
            if (Player.PlaybackState == PlaybackState.Playing)
            {
                PauseAudio();
            }
            else
            {
                PlayAudio();
            }
        }

        public void ExecLoadScale(IScaleLoader loader)
        {
            var fileTypes = loader.SupportedFileExtensions.Select((s) => "*." + LoaderExtentionRegex.Replace(s, "")).ToArray();
            var open = new OpenFileDialog();
            open.Filter = $"Supported File({string.Join(",", fileTypes)})|{string.Join(";", fileTypes)}";
            if (open.ShowDialog() ?? false)
            {
                LoadScale(loader, open.FileName);
            }
        }

        public void ExecLoadScaleFromWave()
        {
            var open = new OpenFileDialog();
            open.Filter = "Wave PCM(*.wav)|*.wav";
            if (open.ShowDialog() ?? false)
            {
                LoadScaleFromWave(open.FileName);
            }
        }

        #endregion

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
            if (!e.Data.GetDataPresent(DataFormats.FileDrop, true) || Lock)
            {
                return;
            }

            // bring window to front
            // see: https://stackoverflow.com/a/4831839
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();

            var filePaths = (e.Data.GetData(DataFormats.FileDrop, true) as string[]) ?? new string[] { "" };
            var extension = Path.GetExtension(filePaths[0]);
            if (extension == ".wav")
            {
                OpenFile(filePaths[0]);
            }
            else if (AnalyzedAudio != null)
            {
                foreach (var loader in ScaleLoaderPlugins)
                {
                    if (loader.SupportedFileExtensions.Select((s) => "." + LoaderExtentionRegex.Replace(s, "")).Contains(extension))
                    {
                        LoadScale(loader, filePaths[0]);
                    }
                }
            }
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Player == null || Lock || e.Key != Key.Space)
            {
                return;
            }
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            Player?.Stop();
            Player?.Dispose();
            SeekPlayer?.Dispose();

            ApplicationSettings.Setting.General.Position = new Point(Left, Top);
            ApplicationSettings.Setting.General.Size = new Size(Width, Height);
            if (WindowState == WindowState.Minimized)
            {
                ApplicationSettings.Setting.General.State = WindowState.Normal;
            }
            else
            {
                ApplicationSettings.Setting.General.State = WindowState;
            }
            ApplicationSettings.Setting.Save();
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
