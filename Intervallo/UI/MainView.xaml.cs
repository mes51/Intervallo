using Intervallo.Cache;
using Intervallo.Model;
using Intervallo.Util;
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
    /// MainView.xaml の相互作用ロジック
    /// </summary>
    public partial class MainView : SampleRangeChangeableControl
    {
        public static readonly DependencyProperty IndicatorPositionProperty = DependencyProperty.Register(
            nameof(IndicatorPosition),
            typeof(int),
            typeof(MainView),
            new PropertyMetadata(0)
        );

        public static readonly DependencyProperty WaveProperty = DependencyProperty.Register(
            nameof(Wave),
            typeof(WaveLineCache),
            typeof(MainView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty AudioScaleProperty = DependencyProperty.Register(
            nameof(AudioScale),
            typeof(AudioScaleModel),
            typeof(MainView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty EditableAudioScaleProperty = DependencyProperty.Register(
            nameof(EditableAudioScale),
            typeof(AudioScaleModel),
            typeof(MainView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty LockProperty = DependencyProperty.Register(
            nameof(Lock),
            typeof(bool),
            typeof(MainView),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange,
                LockChanged
            )
        );

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            nameof(Progress),
            typeof(double),
            typeof(MainView),
            new FrameworkPropertyMetadata(
                0.0,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange
            )
        );

        public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register(
            nameof(MessageText),
            typeof(string),
            typeof(MainView),
            new FrameworkPropertyMetadata(
                "Message...",
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange
            )
        );

        public MainView()
        {
            InitializeComponent();
        }

        public event EventHandler IndicatorMoveStart;

        public event EventHandler IndicatorMoved;

        public event EventHandler IndicatorMoveFinish;

        public override int SampleCount
        {
            get
            {
                return Wave?.SampleCount ?? 0;
            }
        }

        public int IndicatorPosition
        {
            get { return (int)GetValue(IndicatorPositionProperty); }
            set { SetValue(IndicatorPositionProperty, Math.Min(SampleCount, Math.Max(0, value))); }
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

        public bool Lock
        {
            get { return (bool)GetValue(LockProperty); }
            set { SetValue(LockProperty, value); }
        }

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, Math.Min(100.0, Math.Max(0, value))); }
        }

        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value ?? ""); }
        }

        public bool IndicatorIsVisible
        {
            get
            {
                return WaveView.IndicatorIsVisible;
            }
        }

        public void ScrollToIndicatorIfOutOfScreen()
        {
            WaveView.ScrollToIndicatorIfOutOfScreen();
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

        void WaveView_IndicatorMoveStart(object sender, EventArgs e)
        {
            OnIndicatorMoveStart();
        }

        void WaveView_IndicatorMoved(object sender, EventArgs e)
        {
            OnIndicatorMoved();
        }

        void WaveView_IndicatorMoveFinish(object sender, EventArgs e)
        {
            OnIndicatorMoveFinish();
        }

        static void LockChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
