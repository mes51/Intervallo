using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing.Drawing2D;
using Intervallo.Util;
using Intervallo.Model;

namespace Intervallo.UI
{
    public class ScaleCanvas : GDILineDrawableControl
    {
        const double DefaultHeight = 100.0;
        static readonly double Log2 = Math.Log(2.0);
        static readonly DoubleRange MaxRange = new DoubleRange(0.0, 12.0);

        public static readonly DependencyProperty ScaleRangeProperty = DependencyProperty.Register(
            nameof(ScaleRange),
            typeof(DoubleRange),
            typeof(ScaleCanvas),
            new FrameworkPropertyMetadata(
                new DoubleRange(4.5, 5.5),
                FrameworkPropertyMetadataOptions.AffectsRender,
                ScaleRangeChanged
            )
        );

        public static readonly DependencyProperty AudioScaleProperty = DependencyProperty.Register(
            nameof(AudioScale),
            typeof(AudioScaleModel),
            typeof(ScaleCanvas),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                AnalyzedAudioChanged
            )
        );

        public DoubleRange ScaleRange
        {
            get { return (DoubleRange)GetValue(ScaleRangeProperty); }
            set { SetValue(ScaleRangeProperty, value.Adjust(MaxRange)); }
        }

        public AudioScaleModel AudioScale
        {
            get { return (AudioScaleModel)GetValue(AudioScaleProperty); }
            set { SetValue(AudioScaleProperty, value); }
        }

        public override int SampleCount
        {
            get
            {
                return AudioScale?.SampleCount ?? 0;
            }
        }

        protected override double PathHeight
        {
            get
            {
                return DefaultHeight * ScaleRange.Length;
            }
        }

        protected override double BaseY
        {
            get
            {
                return -(12.0 - ScaleRange.End) * DefaultHeight;
            }
        }

        protected override void UpdatePath(GraphicsPath path)
        {
            base.UpdatePath(path);

            if (AudioScale == null)
            {
                return;
            }
            
            if (AudioScale.FrameLength < 2)
            {
                var y = (float)(FreqencyToScale(AudioScale.F0[0]) * DefaultHeight);
                path.AddLine(new System.Drawing.PointF(0.0F, y), new System.Drawing.PointF((float)ActualWidth, y));
            }
            else
            {
                var framePerSample = 1000.0 / AudioScale.SampleRate / AudioScale.FramePeriod;
                var frameCount = Math.Min((int)Math.Ceiling(SampleRange.Length * framePerSample) + 1, AudioScale.FrameLength);
                var begin = (int)Math.Floor(SampleRange.Begin * framePerSample);
                var points = AudioScale.F0
                    .Skip(begin)
                    .Take(frameCount)
                    .Select((f, i) => new { Scale = FreqencyToScale(f), Index = i })
                    .Where((sx) => sx.Scale > 0.0)
                    .Select((sx) => new System.Drawing.PointF(sx.Index, (float)((12.0 - sx.Scale) * DefaultHeight)))
                    .ToArray();

                var groupStartIndex = 0;
                for (var i = 1; i < points.Length; i++)
                {
                    if (points[i].X - points[i - 1].X >= 1.1)
                    {
                        AddScalesToPath(path, points.Skip(groupStartIndex).Take(i - groupStartIndex).ToArray());
                        groupStartIndex = i;
                    }
                }
                AddScalesToPath(path, points.Skip(groupStartIndex).Take(points.Length - groupStartIndex).ToArray());
            }
        }

        protected override double GetSampleProgress()
        {
            if (AudioScale == null)
            {
                return ActualWidth;
            }
            else
            {
                return ActualWidth / Math.Max(1, SampleRange.Length / (AudioScale.SampleRate * AudioScale.FramePeriod * 0.001));
            }
        }

        void AddScalesToPath(GraphicsPath path, System.Drawing.PointF[] scales)
        {
            path.StartFigure();
            if (scales.Length > 1)
            {
                path.AddLines(scales);
            }
            else if (scales.Length > 0)
            {
                path.AddLine(scales[0], new System.Drawing.PointF(scales[0].X + 1.0F, scales[0].Y));
            }
        }

        static double FreqencyToScale(double frequency)
        {
            return (Math.Log(frequency / 440.0) / Log2) + (69.0 / 12.0); // adjust note number (start scale A4 to C-2)
        }

        static void AnalyzedAudioChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((ScaleCanvas)dependencyObject).RefreshPath();
            ((ScaleCanvas)dependencyObject).RedrawBitmap();
        }

        static void ScaleRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((ScaleCanvas)dependencyObject).RedrawBitmap();
        }
    }
}
