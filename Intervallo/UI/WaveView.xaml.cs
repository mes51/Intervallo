using Intervallo.Cache;
using Intervallo.Model;
using Intervallo.Util;
using System;
using System.Collections.Generic;
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
                FrameworkPropertyMetadataOptions.AffectsArrange,
                ViewDependOnPropertyChanged
            )
        );

        public static readonly DependencyProperty IndicatorPositionProperty = DependencyProperty.Register(
            nameof(IndicatorPosition),
            typeof(int),
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsArrange,
                IndicatorPositionChanged
            )
        );

        public static readonly DependencyProperty AudioScaleProperty = DependencyProperty.Register(
            nameof(AudioScale),
            typeof(AudioScaleModel),
            typeof(WaveView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty EditableAudioScaleProperty = DependencyProperty.Register(
            nameof(EditableAudioScale),
            typeof(AudioScaleModel),
            typeof(WaveView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty PreviewableSampleRangesProperty = DependencyProperty.Register(
            nameof(PreviewableSampleRanges),
            typeof(IReadOnlyList<IntRange>),
            typeof(WaveView),
            new FrameworkPropertyMetadata(
                new List<IntRange>(),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange
            )
        );

        readonly Pen Pen = new Pen(new SolidColorBrush(Color.FromRgb(43, 137, 201)), 1.0);

        public WaveView()
        {
            InitializeComponent();
        }

        public WaveLineCache Wave
        {
            get { return (WaveLineCache)GetValue(WaveProperty); }
            set { SetValue(WaveProperty, value); }
        }

        public AudioScaleModel AudioScale
        {
            get { return (AudioScaleModel)GetValue(AudioScaleProperty); }
            set { SetValue(AudioScaleProperty, value); }
        }

        public AudioScaleModel EditableAudioScale
        {
            get { return (AudioScaleModel)GetValue(EditableAudioScaleProperty); }
            set { SetValue(EditableAudioScaleProperty, value); }
        }

        public int IndicatorPosition
        {
            get { return (int)GetValue(IndicatorPositionProperty); }
            set { SetValue(IndicatorPositionProperty, Math.Min(SampleCount, Math.Max(0, value))); }
        }

        public IReadOnlyList<IntRange> PreviewableSampleRanges
        {
            get { return (IReadOnlyList<IntRange>)GetValue(PreviewableSampleRangesProperty); }
            set { SetValue(PreviewableSampleRangesProperty, value); }
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

        IntRange PrevSampleRange { get; set; }

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

            RefreshTimeScrollBar();
            RefreshIndicator();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Focus();

            base.OnMouseLeftButtonDown(e);
        }

        void RefreshTimeScrollBar()
        {
            var scrollable = ScrollableSampleCount;
            TimeScrollBar.Maximum = scrollable;
            TimeScrollBar.IsEnabled = scrollable > 0;
            TimeScrollBar.ViewportSize = TimeScrollBar.IsEnabled ? 1.0 / (TimeScrollBar.Maximum - TimeScrollBar.Minimum) : 0;
            TimeScrollBar.SmallChange = Math.Max(1.0, scrollable / 10000.0);
            TimeScrollBar.LargeChange = Math.Max(1.0, scrollable / 100.0);
            TimeScrollBar.Value = SampleRange.Begin;
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

            var x = e.GetPosition(ClickedElement).X;
            if (ClickedElement == IndicatorMoveArea)
            {
                IndicatorPosition = (int)Math.Round(x / IndicatorMoveArea.ActualWidth * SampleRange.Length) + SampleRange.Begin;
                if (x > IndicatorMoveArea.ActualWidth)
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
                SampleRange = SampleRange.MoveTo(PrevSampleRange.Begin - (int)(move / HandScrollArea.ActualWidth * SampleRange.Length));
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
            RefreshTimeScrollBar();
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
            ClickPosition = e.GetPosition(ClickedElement);
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
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var scaleRange = ScaleMeasureView.ScaleRange;
                var stretch = 0.0;
                if (e.Delta < 0)
                {
                    stretch = Math.Min(scaleRange.Length * 1.1, ScaleMeasureView.MaxOctave) - scaleRange.Length;
                }
                else
                {
                    stretch = Math.Max(scaleRange.Length * 0.9, ScaleMeasureView.ScaleGap) - scaleRange.Length;
                }
                ScaleMeasureView.ScaleRange = scaleRange.Stretch(stretch).Move(stretch * -0.5);
                ScaleScrollBar.SmallChange = ScaleMeasureView.ScaleRange.Length * 0.05;
                ScaleScrollBar.LargeChange = ScaleMeasureView.ScaleRange.Length * 0.5;
                ScaleScrollBar.Maximum = ScaleMeasureView.MaxOctave - ScaleMeasureView.ScaleRange.Length;
                ScaleScrollBar.Value = ScaleMeasureView.ScaleRange.Begin;
                ScaleScrollBar.IsEnabled = ScaleMeasureView.ScaleRange.Length < ScaleMeasureView.MaxOctave;
                ScaleScrollBar.ViewportSize = ScaleScrollBar.IsEnabled ? 1.0 / (ScaleScrollBar.Maximum - ScaleScrollBar.Minimum) : 0;
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                ScaleScrollBar.Value += ScaleScrollBar.SmallChange * Math.Sign(e.Delta);
            }
        }

        void TimeScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Wave == null)
            {
                return;
            }

            SampleRange = SampleRange.MoveTo((int)e.NewValue);
        }

        void ScaleScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ScaleMeasureView != null)
            {
                ScaleMeasureView.ScaleRange = ScaleMeasureView.ScaleRange.MoveTo(ScaleScrollBar.Value);
            }
        }

        static void IndicatorPositionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as WaveView).RefreshIndicator();
        }

        static void ViewDependOnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((WaveView)dependencyObject).SampleCount = ((WaveLineCache)e.NewValue)?.SampleCount ?? 0;
            ((WaveView)dependencyObject).RefreshTimeScrollBar();
            ((WaveView)dependencyObject).RefreshIndicator();
        }
    }
}
