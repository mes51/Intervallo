using Intervallo.Audio;
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
    public partial class WaveScaler : SampleRangeChangeableControl
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

        public WaveScaler()
        {
            InitializeComponent();
        }

        public WaveCache Wave
        {
            get { return (WaveCache)GetValue(WaveProperty); }
            set { SetValue(WaveProperty, value); }
        }

        public override int SampleCount
        {
            get
            {
                return Wave?.Wave.Length ?? 0;
            }
        }

        FrameworkElement ClickedElement { get; set; }

        Point ClickPosition { get; set; } = new Point();

        IntRange PrevSampleRange { get; set; } = new IntRange();

        protected override void OnSampleRangeChanged()
        {
            base.OnSampleRangeChanged();

            UpdateScaler();
        }

        void UpdateScaler()
        {
            WaveCanvas.SampleRange = 0.To(SampleCount);
            var scalerSize =  ActualWidth * SampleRange.Length / SampleCount;
            var scalerPos = ActualWidth * SampleRange.Begin / SampleCount;
            if (SampleCount < 1)
            {
                scalerSize = ActualWidth;
                scalerPos = 0;
            }
            Scaler.Margin = new Thickness(scalerPos, 0, ActualWidth - scalerSize - scalerPos, 0);
        }

        void MoveSampleRange(double mouseX)
        {
            var targetSample = (int)Math.Round(mouseX / ActualWidth * SampleCount);
            SampleRange = SampleRange.MoveTo(targetSample - SampleRange.Length / 2);
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
                var minDiff = MinScalerWidth - ActualWidth * PrevSampleRange.Length / SampleCount;
                if (ClickedElement == RightScale)
                {
                    var diffX = Math.Max(pos.X - ClickPosition.X, minDiff);
                    var moveSample = Math.Min((int)Math.Round(diffX / ActualWidth * SampleCount), SampleCount - PrevSampleRange.End);
                    SampleRange = PrevSampleRange.Stretch(moveSample);
                }
                else if (ClickedElement == LeftScale)
                {
                    var diffX = Math.Max(ClickPosition.X - pos.X, minDiff);
                    var moveSample = Math.Min((int)Math.Round(diffX / ActualWidth * SampleCount), PrevSampleRange.Begin);
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

        static void ViewDependOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as WaveScaler).UpdateScaler();
        }
    }
}
