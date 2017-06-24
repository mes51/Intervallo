using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Intervallo.UI
{
    /// <summary>
    /// WaveScaler.xaml の相互作用ロジック
    /// </summary>
    public partial class WaveScaler : UserControl
    {
        const int MinShowableSampleCount = 100;

        public static readonly DependencyProperty WaveProperty = WaveCanvas.WaveProperty.AddOwner(
            typeof(WaveScaler),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty ViewStartSampleProperty = DependencyProperty.Register(
            nameof(ViewStartSample),
            typeof(int),
            typeof(WaveScaler),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty ShowableSampleCountProperty = DependencyProperty.Register(
            nameof(ShowableSampleCount),
            typeof(int),
            typeof(WaveScaler),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public WaveScaler()
        {
            InitializeComponent();
        }

        public double[] Wave
        {
            get { return (double[])GetValue(WaveProperty); }
            set { SetValue(WaveProperty, value); }
        }

        public int ViewStartSample
        {
            get { return (int)GetValue(ViewStartSampleProperty); }
            set { SetValue(ViewStartSampleProperty, Math.Min(ScrollableSampleCount, Math.Max(0, value))); }
        }

        public int ShowableSampleCount
        {
            get { return (int)GetValue(ShowableSampleCountProperty); }
            set { SetValue(ShowableSampleCountProperty, Math.Max(Math.Min(MinShowableSampleCount, WaveSampleCount), value)); }
        }

        public int WaveSampleCount
        {
            get
            {
                return Wave?.Length ?? 0;
            }
        }

        public int ScrollableSampleCount
        {
            get
            {
                return Math.Max(0, WaveSampleCount - ShowableSampleCount);
            }
        }

        FrameworkElement ClickedElement { get; set; }

        Point ClickPosition { get; set; } = new Point();

        int PrevViewStartSample { get; set; }

        int PrevShowableSampleCount { get; set; }

        void UpdateScaler()
        {
            WaveCanvas.ShowableSampleCount = WaveSampleCount;
            var scalerSize =  ActualWidth * ShowableSampleCount / WaveSampleCount;
            var scalerPos = ActualWidth * ViewStartSample / WaveSampleCount;
            if (WaveSampleCount < 1)
            {
                scalerSize = ActualWidth;
                scalerPos = 0;
            }
            Scaler.Margin = new Thickness(scalerPos, 0, ActualWidth - scalerSize - scalerPos, 0);
        }

        void MoveViewStartSample(double mouseX)
        {
            var targetSample = (int)Math.Round(mouseX / ActualWidth * WaveSampleCount);
            ViewStartSample = targetSample - ShowableSampleCount / 2;
        }

        void WaveScaler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickedElement = e.OriginalSource as FrameworkElement;
            ClickPosition = e.GetPosition(this);
            PrevViewStartSample = ViewStartSample;
            PrevShowableSampleCount = ShowableSampleCount;
            Mouse.Capture(this, CaptureMode.Element);
            if (ClickedElement != RightScale && ClickedElement != LeftScale)
            {
                MoveViewStartSample(e.GetPosition(this).X);
            }
        }

        void WaveScaler_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                if (ClickedElement == RightScale)
                {
                    var diffX = pos.X - ClickPosition.X;
                    var moveSample = (int)Math.Round(diffX / ActualWidth * WaveSampleCount);
                    ShowableSampleCount = PrevShowableSampleCount + moveSample;
                }
                else if (ClickedElement == LeftScale)
                {
                    var diffX = ClickPosition.X - pos.X;
                    var moveSample = (int)Math.Round(diffX / ActualWidth * WaveSampleCount);
                    ShowableSampleCount = PrevShowableSampleCount + moveSample;
                    ViewStartSample = PrevViewStartSample - moveSample;
                }
                else
                {
                    MoveViewStartSample(pos.X);
                }
            }
            else
            {
                ClickedElement = null;
            }
        }

        void WaveScaler_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
        }

        static void ViewDependOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as WaveScaler).UpdateScaler();
        }
    }
}
