using Intervallo.Audio;
using Intervallo.Markup;
using Intervallo.Properties;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Intervallo.Form
{
    /// <summary>
    /// WaveExportSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WaveExportSettingWindow : Window
    {
        public static readonly DependencyProperty SavePathProperty = DependencyProperty.Register(
            nameof(SavePath),
            typeof(string),
            typeof(WaveExportSettingWindow),
            new FrameworkPropertyMetadata(
                "",
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange
            )
        );

        public static readonly DependencyProperty SelectedWaveBitValueProperty = DependencyProperty.Register(
            nameof(SelectedWaveBitValue),
            typeof(Tuple<Enum, string>),
            typeof(WaveExportSettingWindow),
            new FrameworkPropertyMetadata(
                EnumerationLangKeyExtension.CreateTuple(WaveBit.Bit16),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange
            )
        );

        public WaveExportSettingWindow()
        {
            InitializeComponent();
        }

        public string SavePath
        {
            get { return (string)GetValue(SavePathProperty); }
            set { SetValue(SavePathProperty, value); }
        }

        public Tuple<Enum, string> SelectedWaveBitValue
        {
            get { return (Tuple<Enum, string>)GetValue(SelectedWaveBitValueProperty); }
            set { SetValue(SelectedWaveBitValueProperty, value); }
        }

        public WaveBit SelectedWaveBit
        {
            get
            {
                return (WaveBit)SelectedWaveBitValue.Item1;
            }
            set
            {
                SelectedWaveBitValue = EnumerationLangKeyExtension.CreateTuple(value);
            }
        }

        private void SelectPathButton_Click(object sender, RoutedEventArgs e)
        {
            var save = new SaveFileDialog();
            save.Filter = "Wave PCM(*.wav)|*.wav";
            if (save.ShowDialog() ?? false)
            {
                SavePath = save.FileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SavePath))
            {
                MessageBox.ShowError(LangResources.WaveExportSettingWindow_EmptySavePathMessage);
                return;
            }
            try
            {
                Path.GetFullPath(SavePath);
            }
            catch
            {
                MessageBox.ShowError(string.Format(LangResources.WaveExportSettingWindow_InvalidSavePathMessage, SavePath));
                return;
            }
            if (!Directory.Exists(Path.GetDirectoryName(SavePath)))
            {

                MessageBox.ShowError(string.Format(LangResources.WaveExportSettingWindow_DirectoryNotFoundMessage, SavePath, Path.GetDirectoryName(SavePath)));
                return;
            }
            if (string.IsNullOrEmpty(Path.GetFileName(SavePath)))
            {
                MessageBox.ShowError(string.Format(LangResources.WaveExportSettingWindow_EmptyFileNameMessage, SavePath));
                return;
            }
            if (File.Exists(SavePath))
            {
                var messageBox = MessageBox.CreateWarning(string.Format(LangResources.WaveExportSettingWindow_OverWriteCautionMessage, SavePath), LangResources.WaveExportSettingWindow_OverWriteTitle, MessageBoxButton.YesNo);
                messageBox.AddYesAction(() =>
                {
                    DialogResult = true;
                    Close();
                });
                messageBox.Show();
            }
            else
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
