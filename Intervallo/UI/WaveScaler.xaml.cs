using Intervallo.Util;
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
        const double MinScalerWidth = 5.0;

        public static readonly DependencyProperty WaveProperty = WaveCanvas.WaveProperty.AddOwner(
            typeof(WaveScaler),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty SampleRangeProperty = DependencyProperty.Register(
            nameof(SampleRange),
            typeof(Range),
            typeof(WaveScaler),
            new FrameworkPropertyMetadata(
                new Range(),
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

        public Range SampleRange
        {
            get { return (Range)GetValue(SampleRangeProperty); }
            set { SetValue(SampleRangeProperty, value.Adjust(0.To(WaveSampleCount))); }
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
                return Math.Max(0, WaveSampleCount - SampleRange.Length);
            }
        }

        FrameworkElement ClickedElement { get; set; }

        Point ClickPosition { get; set; } = new Point();

        Range PrevSampleRange { get; set; } = new Range();

        void UpdateScaler()
        {
            WaveCanvas.SampleRange = 0.To(WaveSampleCount);
            var scalerSize =  ActualWidth * SampleRange.Length / WaveSampleCount;
            var scalerPos = ActualWidth * SampleRange.Begin / WaveSampleCount;
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
            SampleRange = SampleRange.MoveTo(targetSample - SampleRange.Length / 2);
        }

        void WaveScaler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickedElement = e.OriginalSource as FrameworkElement;
            ClickPosition = e.GetPosition(this);
            PrevSampleRange = SampleRange;
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
                var minDiff = MinScalerWidth - ActualWidth * PrevSampleRange.Length / WaveSampleCount;
                if (ClickedElement == RightScale)
                {
                    var diffX = Math.Max(pos.X - ClickPosition.X, minDiff);
                    var moveSample = Math.Min((int)Math.Round(diffX / ActualWidth * WaveSampleCount), WaveSampleCount - PrevSampleRange.End);
                    SampleRange = PrevSampleRange.Stretch(moveSample);
                }
                else if (ClickedElement == LeftScale)
                {
                    var diffX = Math.Max(ClickPosition.X - pos.X, minDiff);
                    var moveSample = Math.Min((int)Math.Round(diffX / ActualWidth * WaveSampleCount), PrevSampleRange.Begin);
                    SampleRange = PrevSampleRange.Stretch(moveSample).Move(-moveSample);
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
