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
        const int MinSampleCount = 5;

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

        void MoveSampleRange(double mouseX)
        {
            var targetSample = (int)Math.Round(mouseX / ActualWidth * WaveSampleCount);
            SampleRange = SampleRange.MoveTo(targetSample - SampleRange.Length / 2);
        }

        void ScrollSample(int direction)
        {
            SampleRange = SampleRange.Move((int)Math.Ceiling(SampleRange.Length * 0.1) * Math.Sign(direction));
        }

        void WaveScaler_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Wave == null)
            {
                return;
            }

            ClickedElement = e.OriginalSource as FrameworkElement;
            var clicked = ClickedElement;
            while (clicked != null && clicked != RightScale && clicked != LeftScale)
            {
                clicked = clicked.Parent as FrameworkElement;
            }
            if (clicked != null)
            {
                ClickedElement = clicked;
            }

            ClickPosition = e.GetPosition(this);
            PrevSampleRange = SampleRange;
            Mouse.Capture(this, CaptureMode.Element);
            if (ClickedElement != RightScale && ClickedElement != LeftScale)
            {
                MoveSampleRange(e.GetPosition(this).X);
            }
        }

        void WaveScaler_MouseMove(object sender, MouseEventArgs e)
        {
            if (Wave != null && e.LeftButton == MouseButtonState.Pressed)
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
                    MoveSampleRange(pos.X);
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

        void WaveScaler_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Wave == null)
            {
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                ScrollSample(-e.Delta);
            }
            else
            {
                if (e.Delta > 0)
                {
                    var stretch = (int)Math.Ceiling((SampleRange.Length * 1.1)) - SampleRange.Length;
                    SampleRange = SampleRange.Stretch(stretch).Move(stretch / -2);
                }
                else
                {
                    var stretch = Math.Max((int)(SampleRange.Length * 0.9), MinSampleCount) - SampleRange.Length;
                    SampleRange = SampleRange.Stretch(stretch).Move(stretch / -2);
                }
            }
        }

        void MouseTiltWheelBehavior_MouseTiltWheel(object sender, Behavior.MouseTiltWheelEventArgs e)
        {
            if (Wave == null)
            {
                return;
            }

            ScrollSample(e.Delta);
        }

        static void ViewDependOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as WaveScaler).UpdateScaler();
        }
    }
}
