using System;
using System.Collections.Generic;
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
    /// WaveView.xaml の相互作用ロジック
    /// </summary>
    public partial class WaveView : UserControl
    {
        public static readonly DependencyProperty WaveProperty = WaveCanvas.WaveProperty.AddOwner(
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty ViewStartSampleProperty = WaveCanvas.ViewStartSampleProperty.AddOwner(
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewStartSampleChanged
            )
        );

        public static readonly DependencyProperty ShowableSampleCountProperty = WaveCanvas.ShowableSampleCountProperty.AddOwner(
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty SampleRateProperty = MeasureView.SampleRateProperty.AddOwner(
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty IndicatorPositionProperty = DependencyProperty.Register(
            nameof(IndicatorPosition),
            typeof(int),
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                IndicatorPositionChanged
            )
        );

        readonly Pen Pen = new Pen(new SolidColorBrush(Color.FromRgb(43, 137, 201)), 1.0);

        public WaveView()
        {
            InitializeComponent();
        }

        public double[] Wave
        {
            get { return (double[])GetValue(WaveProperty); }
            set { SetValue(WaveProperty, value); }
        }

        public int ShowableSampleCount
        {
            get { return (int)GetValue(ShowableSampleCountProperty); }
            set { SetValue(ShowableSampleCountProperty, value); }
        }

        public int ViewStartSample
        {
            get { return (int)GetValue(ViewStartSampleProperty); }
            set { SetValue(ViewStartSampleProperty, Math.Min(ScrollableSampleCount, Math.Max(0, value))); }
        }

        public int SampleRate
        {
            get { return (int)GetValue(SampleRateProperty); }
            set { SetValue(SampleRateProperty, value); }
        }

        public int IndicatorPosition
        {
            get { return (int)GetValue(IndicatorPositionProperty); }
            set { SetValue(IndicatorPositionProperty, Math.Min(ScrollableSampleCount, Math.Max(0, value))); }
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

        public bool IndicatorIsVisible
        {
            get
            {
                var center = Indicator.ActualWidth * 0.5;
                var x = Canvas.GetLeft(Indicator);
                return x >= -center && x <= IndicatorCanvas.ActualWidth - center;
            }
        }

        public event EventHandler IndicatorMoveStart;

        public event EventHandler IndicatorMoved;

        public event EventHandler IndicatorMoveFinish;

        FrameworkElement ClickedElement { get; set; }

        public void ScrollToIndicatorIfOutOfScreen()
        {
            if (!IndicatorIsVisible)
            {
                ViewStartSample = IndicatorPosition;
            }
        }

        void RefreshScrollBar()
        {
            var scrollable = ScrollableSampleCount;
            ScrollBar.Maximum = scrollable;
            ScrollBar.IsEnabled = scrollable > 0;
            ScrollBar.ViewportSize = ScrollBar.IsEnabled ? 1.0 / (ScrollBar.Maximum - ScrollBar.Minimum) : 0;
            ScrollBar.LargeChange = Math.Max(1.0, scrollable / 100.0);
        }

        void RefreshIndicator()
        {
            var center = Indicator.ActualWidth * 0.5;
            var x = IndicatorCanvas.ActualWidth / ShowableSampleCount * (IndicatorPosition - ViewStartSample) - center;
            Canvas.SetLeft(Indicator, x);
            Indicator.Visibility = IndicatorIsVisible ? Visibility.Visible : Visibility.Hidden;
            UpdateLayout();
        }

        void MouseMoveHandler(MouseEventArgs e)
        {
            if (ClickedElement == IndicatorMoveArea)
            {
                if (Wave == null)
                {
                    return;
                }

                var x = e.GetPosition(this).X;
                IndicatorPosition = (int)Math.Round(x / ActualWidth * ShowableSampleCount) + ViewStartSample;
                if (x > ActualWidth)
                {
                    ViewStartSample = IndicatorPosition - ShowableSampleCount;
                }
                else if (x < 0.0)
                {
                    ViewStartSample = IndicatorPosition;
                }
                OnIndicatorMoved();
            }
        }

        void OnIndicatorMoveStart()
        {
            IndicatorMoveStart?.Invoke(this, EventArgs.Empty);
        }

        void OnIndicatorMoved()
        {
            IndicatorMoved?.Invoke(this, EventArgs.Empty);
        }

        void OnIndicatorMoveFinish()
        {
            IndicatorMoveFinish?.Invoke(this, EventArgs.Empty);
        }

        void WaveView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshScrollBar();
        }

        void WaveView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                MouseMoveHandler(e);
            }
        }

        void WaveView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickedElement = e.OriginalSource as FrameworkElement;
            Mouse.Capture(this);

            if (ClickedElement == IndicatorMoveArea)
            {
                OnIndicatorMoveStart();
            }

            MouseMoveHandler(e);
        }

        void WaveView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();

            if (ClickedElement == IndicatorMoveArea)
            {
                OnIndicatorMoveFinish();
            }

            ClickedElement = null;
        }

        void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ViewStartSample = (int)e.NewValue;
        }

        static void IndicatorPositionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as WaveView).RefreshIndicator();
        }

        static void ViewDependOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as WaveView).RefreshScrollBar();
            (dependencyObject as WaveView).RefreshIndicator();
        }

        static void ViewStartSampleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var waveView = dependencyObject as WaveView;
            waveView.ScrollBar.Value = waveView.ViewStartSample;
            waveView.RefreshIndicator();
        }
    }
}
