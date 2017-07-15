using Intervallo.Util;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        public static readonly DependencyProperty SampleRangeProperty = WaveCanvas.SampleRangeProperty.AddOwner(
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                new Range(),
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

        const int MinSampleCount = 5;

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

        public Range SampleRange
        {
            get { return (Range)GetValue(SampleRangeProperty); }
            set { SetValue(SampleRangeProperty, value.Adjust(0.To(WaveSampleCount))); }
        }

        public int SampleRate
        {
            get { return (int)GetValue(SampleRateProperty); }
            set { SetValue(SampleRateProperty, value); }
        }

        public int IndicatorPosition
        {
            get { return (int)GetValue(IndicatorPositionProperty); }
            set { SetValue(IndicatorPositionProperty, Math.Min(WaveSampleCount, Math.Max(0, value))); }
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
                SampleRange = SampleRange.MoveTo(IndicatorPosition);
            }
        }

        void RefreshScrollBar()
        {
            var scrollable = ScrollableSampleCount;
            ScrollBar.Maximum = scrollable;
            ScrollBar.IsEnabled = scrollable > 0;
            ScrollBar.ViewportSize = ScrollBar.IsEnabled ? 1.0 / (ScrollBar.Maximum - ScrollBar.Minimum) : 0;
            ScrollBar.LargeChange = Math.Max(1.0, scrollable / 100.0);
            ScrollBar.Value = SampleRange.Begin;
        }

        void RefreshIndicator()
        {
            var center = Indicator.ActualWidth * 0.5;
            var x = IndicatorCanvas.ActualWidth / SampleRange.Length * (IndicatorPosition - SampleRange.Begin) - center;
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
                IndicatorPosition = (int)Math.Round(x / ActualWidth * SampleRange.Length) + SampleRange.Begin;
                if (x > ActualWidth)
                {
                    SampleRange = SampleRange.MoveTo(IndicatorPosition - SampleRange.Length);
                }
                else if (x < 0.0)
                {
                    SampleRange = SampleRange.MoveTo(IndicatorPosition);
                }
                OnIndicatorMoved();
            }
        }

        void ScrollSample(int direction)
        {
            SampleRange = SampleRange.Move((int)Math.Ceiling(SampleRange.Length * 0.1) * Math.Sign(direction));
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

        void WaveView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
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

        void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SampleRange = SampleRange.MoveTo((int)e.NewValue);
        }

        void MouseTiltWheelBehavior_MouseTiltWheel(object sender, Behavior.MouseTiltWheelEventArgs e)
        {
            ScrollSample(e.Delta);
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
    }
}
