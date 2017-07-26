using Intervallo.Converter;
using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Intervallo.UI
{
    public enum OctaveType : int
    {
        Yamaha = -2,
        International = -1
    }

    public class ScaleMeasureView : UserControl
    {
        public const int MaxOctave = 12;
        public const double ScaleGap = 1.0 / 12.0;

        const int TotalScales = 12 * 12;
        const double LabelGap = 4.0;
        readonly string[] ScaleNames = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        readonly DoubleRange MaxRange = new DoubleRange(0.0, 12.0);

        public static readonly DependencyProperty ScaleRangeProperty = DependencyProperty.Register(
            nameof(ScaleRange),
            typeof(DoubleRange),
            typeof(ScaleMeasureView),
            new FrameworkPropertyMetadata(
                new DoubleRange(4.5, 5.5),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty OctaveTypeProperty = DependencyProperty.Register(
            nameof(OctaveType),
            typeof(OctaveType),
            typeof(ScaleMeasureView),
            new FrameworkPropertyMetadata(
                OctaveType.Yamaha,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty ScalePenProperty = DependencyProperty.Register(
            nameof(ScalePen),
            typeof(Pen),
            typeof(ScaleMeasureView),
            new FrameworkPropertyMetadata(
                new Pen(new SolidColorBrush(Colors.Black), 1.0),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty OctavePenProperty = DependencyProperty.Register(
            nameof(OctavePen),
            typeof(Pen),
            typeof(ScaleMeasureView),
            new FrameworkPropertyMetadata(
                new Pen(new SolidColorBrush(Colors.Black), 1.0),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty ScaleTextBrushProperty = DependencyProperty.Register(
            nameof(ScaleTextBrush),
            typeof(Brush),
            typeof(ScaleMeasureView),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Colors.Black),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty ScaleLabelAreaProperty = DependencyProperty.Register(
            nameof(ScaleLabelArea),
            typeof(double),
            typeof(ScaleMeasureView),
            new FrameworkPropertyMetadata(
                35.0,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        static ScaleMeasureView()
        {
            PenConverter.Register();
        }

        public ScaleMeasureView()
        {
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        public DoubleRange ScaleRange
        {
            get { return (DoubleRange)GetValue(ScaleRangeProperty); }
            set { SetValue(ScaleRangeProperty, value.Adjust(MaxRange)); }
        }

        public OctaveType OctaveType
        {
            get { return (OctaveType)GetValue(OctaveTypeProperty); }
            set { SetValue(OctaveTypeProperty, value); }
        }

        [TypeConverter(typeof(PenConverter))]
        public Pen ScalePen
        {
            get { return (Pen)GetValue(ScalePenProperty); }
            set { SetValue(ScalePenProperty, value); }
        }

        [TypeConverter(typeof(PenConverter))]
        public Pen OctavePen
        {
            get { return (Pen)GetValue(OctavePenProperty); }
            set { SetValue(OctavePenProperty, value); }
        }

        public Brush ScaleTextBrush
        {
            get { return (Brush)GetValue(ScaleTextBrushProperty); }
            set { SetValue(ScaleTextBrushProperty, value); }
        }

        public double ScaleLabelArea
        {
            get { return (double)GetValue(ScaleLabelAreaProperty); }
            set { SetValue(ScaleLabelAreaProperty, value); }
        }

        Typeface Typeface => new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            
            var scalePerHeight = ScaleGap / ScaleRange.Length * ActualHeight;
            var startY = (ScaleRange.End - 12.0) * scalePerHeight * 12.0;
            var octaveIndexStart = (int)OctaveType;
            var measureText = CreateScaleText("G#-2");
            var drawOctaveOnly = measureText.Height >= scalePerHeight;
            var clip = new Rect(0.0, 0.0, ActualWidth, ActualHeight);

            drawingContext.PushClip(new RectangleGeometry(clip));

            for (var i = 0; i <= TotalScales; i++)
            {
                var y = startY + i * scalePerHeight;
                if (y >= 0.0 && y <= ActualHeight)
                {
                    drawingContext.DrawLine(i % 12 == 0 ? OctavePen : ScalePen, new Point(ScaleLabelArea - 5.0, y), new Point(ActualWidth, y));
                }

                var scaleIndex = TotalScales - i;
                if (!drawOctaveOnly || scaleIndex % 12 == 0)
                {
                    var octave = scaleIndex / 12 + octaveIndexStart;
                    var scaleText = CreateScaleText(ScaleNames[scaleIndex % 12] + octave);
                    var pos = new Point(LabelGap, y - scaleText.Height * 0.5);
                    if (clip.IntersectsWith(new Rect(pos, new Size(scaleText.Width, scaleText.Height))))
                    {
                        drawingContext.DrawText(scaleText, new Point(pos.X, Math.Min(Math.Max(pos.Y, 0.0), ActualHeight - scaleText.Height)));
                    }
                }
            }
            drawingContext.DrawLine(ScalePen, new Point(ScaleLabelArea, 0.0), new Point(ScaleLabelArea, ActualHeight));

            drawingContext.Pop();
        }

        FormattedText CreateScaleText(string scale)
        {
            return new FormattedText(scale, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface, 10.0, ScaleTextBrush);
        }
    }
}
