using Intervallo.Audio;
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
    public partial class WaveView : SampleRangeChangeableControl
    {
        public static readonly DependencyProperty WaveProperty = WaveCanvas.WaveProperty.AddOwner(
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                null,
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

        public WaveData Wave
        {
            get { return (WaveData)GetValue(WaveProperty); }
            set { SetValue(WaveProperty, value); }
        }

        public int IndicatorPosition
        {
            get { return (int)GetValue(IndicatorPositionProperty); }
            set { SetValue(IndicatorPositionProperty, Math.Min(SampleCount, Math.Max(0, value))); }
        }

        public override int SampleCount
        {
            get
            {
                return Wave?.Wave.Length ?? 0;
            }
        }

        public bool IndicatorIsVisible
        {
            get
            {
                var center = Indicator.ActualWidth * 0.5;
                var x = Canvas.GetLeft(Indicator);
                return x > -center - 0.5 && x < IndicatorCanvas.ActualWidth - center + 0.5;
            }
        }

        public event EventHandler IndicatorMoveStart;

        public event EventHandler IndicatorMoved;

        public event EventHandler IndicatorMoveFinish;

        Point ClickPosition { get; set; }

        FrameworkElement ClickedElement { get; set; }

        Range PrevSampleRange { get; set; }

        public void ScrollToIndicatorIfOutOfScreen()
        {
            if (!IndicatorIsVisible)
            {
                SampleRange = SampleRange.MoveTo(IndicatorPosition);
            }
        }

        protected override void OnSampleRangeChanged()
        {
            base.OnSampleRangeChanged();

            RefreshScrollBar();
            RefreshIndicator();
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
            if (Wave == null)
            {
                return;
            }

            var x = e.GetPosition(this).X;
            if (ClickedElement == IndicatorMoveArea)
            {
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
            else if (ClickedElement == HandScrollArea)
            {
                var move = x - ClickPosition.X;
                SampleRange = SampleRange.MoveTo(PrevSampleRange.Begin - (int)(move / ActualWidth * SampleRange.Length));
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
            ClickPosition = e.GetPosition(this);
            PrevSampleRange = SampleRange;
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
            if (Wave == null)
            {
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
            }
        }

        void ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Wave == null)
            {
                return;
            }

            SampleRange = SampleRange.MoveTo((int)e.NewValue);
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
