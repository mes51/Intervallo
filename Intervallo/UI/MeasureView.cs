using Intervallo.Converter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Intervallo.UI
{
    public class MeasureView : SampleRangeChangeableControl
    {
        class BorderScale
        {
            public double Threshold { get; set; }
            public double Scale { get; set; }
            public int SubBorderCount { get; set; }
        }

        const double TimeHorizontalGap = 20.0;
        static readonly double Log60 = Math.Log(60.0);
        static readonly BorderScale[] LargeBorderScales = new BorderScale[]
        {
            new BorderScale() { Threshold = 7.5, Scale = 10.0, SubBorderCount = 0 },
            new BorderScale() { Threshold = 5.0, Scale = 10.0, SubBorderCount = 1 },
            new BorderScale() { Threshold = 3.0, Scale = 5.0, SubBorderCount = 4 },
            new BorderScale() { Threshold = 0.0, Scale = 3.0, SubBorderCount = 2 },
        };
        static readonly BorderScale[] SmallBorderScales = new BorderScale[]
        {
            new BorderScale() { Threshold = 7.5, Scale = 10.0, SubBorderCount = 0 },
            new BorderScale() { Threshold = 5.0, Scale = 10.0, SubBorderCount = 1 },
            new BorderScale() { Threshold = 2.0, Scale = 5.0, SubBorderCount = 4 },
            new BorderScale() { Threshold = 0.0, Scale = 2.0, SubBorderCount = 1 },
        };

        public static readonly DependencyProperty SampleRateProperty = DependencyProperty.Register(
            nameof(SampleRate),
            typeof(int),
            typeof(MeasureView),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty HeaderHeightProperty = DependencyProperty.Register(
            nameof(HeaderHeight),
            typeof(double),
            typeof(MeasureView),
            new FrameworkPropertyMetadata(
                25.0,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty MeasurePenProperty = DependencyProperty.Register(
            nameof(MeasurePen),
            typeof(Pen),
            typeof(MeasureView),
            new FrameworkPropertyMetadata(
                new Pen(new SolidColorBrush(Colors.Black), 1.0),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty TimeTextBrushProperty = DependencyProperty.Register(
            nameof(TimeTextBrush),
            typeof(Brush),
            typeof(MeasureView),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Colors.Black),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        static MeasureView()
        {
            PenConverter.Register();
        }

        public int SampleRate
        {
            get { return (int)GetValue(SampleRateProperty); }
            set { SetValue(SampleRateProperty, value); }
        }

        public double HeaderHeight
        {
            get { return (double)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        [TypeConverter(typeof(PenConverter))]
        public Pen MeasurePen
        {
            get { return (Pen)GetValue(MeasurePenProperty); }
            set { SetValue(MeasurePenProperty, value); }
        }

        public Brush TimeTextBrush
        {
            get { return (Brush)GetValue(TimeTextBrushProperty); }
            set { SetValue(TimeTextBrushProperty, value); }
        }

        Typeface Typeface => new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight)));

            var textWidth = CreateTimecodeText(new TimeSpan(2)).Width * 2.0 + TimeHorizontalGap;
            var sampleInterval = ActualWidth / SampleRange.Length;
            var timePerSample = 1.0 / SampleRate;
            if (double.IsInfinity(sampleInterval) || double.IsInfinity(timePerSample))
            {
                sampleInterval = 0.01;
                timePerSample = 0.01;
            }

            var minTextTimeInterval = textWidth / sampleInterval * timePerSample;
            var scaledTimeInterval = minTextTimeInterval;
            var timeUnit = 1.0;
            var borderInterval = 0.0;
            var subBorderCount = 0;
            var pow = 1.0;
            if (minTextTimeInterval >= 1.0)
            {
                pow = Math.Pow(60.0, (int)Math.Floor(Math.Log(scaledTimeInterval) / Log60));
                scaledTimeInterval /= pow;
                timeUnit *= pow;
            }
            pow = Math.Pow(10.0, (int)Math.Floor(Math.Log10(scaledTimeInterval)));
            scaledTimeInterval /= pow;
            timeUnit *= pow;
            foreach (var b in minTextTimeInterval >= 1.0 ? LargeBorderScales : SmallBorderScales)
            {
                if (scaledTimeInterval > b.Threshold)
                {
                    borderInterval = b.Scale / scaledTimeInterval * textWidth;
                    timeUnit *= b.Scale;
                    subBorderCount = b.SubBorderCount;
                    break;
                }
            }

            var subBorderInterval = borderInterval / (subBorderCount + 1);
            var startTime = (int)((SampleRange.Begin * timePerSample) / timeUnit);
            var startX = -((SampleRange.Begin * sampleInterval) % borderInterval);
            var borderCount = Math.Ceiling(ActualWidth / borderInterval) + 1;
            for (var i = 0; i < borderCount; i++)
            {
                var x = startX + borderInterval * i;
                var time = CreateTimecodeText(new TimeSpan((long)Math.Round(TimeSpan.TicksPerSecond * timeUnit * (startTime + i))));
                var textTop = Math.Max(0.0, HeaderHeight - time.Height - 5.0) * 0.5;
                var textX = x - time.Width * 0.5;

                if (startTime + i > 0)
                {
                    drawingContext.DrawText(time, new Point(textX, textTop));
                }
                drawingContext.DrawLine(MeasurePen, new Point(x, HeaderHeight - 5.0), new Point(x, ActualHeight));
                for (var n = 1; n <= subBorderCount; n++)
                {
                    var sx = x + subBorderInterval * n;
                    drawingContext.DrawLine(MeasurePen, new Point(sx, HeaderHeight - 5.0), new Point(sx, HeaderHeight));
                }
            }

            drawingContext.DrawLine(MeasurePen, new Point(0.0, HeaderHeight), new Point(ActualWidth, HeaderHeight));

            drawingContext.Pop();
        }

        FormattedText CreateTimecodeText(TimeSpan time)
        {
            var hour = (24 * time.Days + time.Hours).ToString("D2");
            var minuets = time.Minutes.ToString("D2");
            var second = time.Seconds.ToString("D2");
            var smallTime = ((time.Ticks % TimeSpan.TicksPerSecond) / (double)TimeSpan.TicksPerSecond).ToString("F5").Substring(2);
            var timeText = $"{hour}:{minuets}:{second}.{smallTime}";
            return new FormattedText(timeText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface, 10.0, TimeTextBrush);
        }
    }
}
