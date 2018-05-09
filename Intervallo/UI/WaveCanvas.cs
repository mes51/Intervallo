using Intervallo.Cache;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Intervallo.UI
{
    public partial class WaveCanvas : GDILineDrawableControl
    {
        public static readonly DependencyProperty WaveProperty = DependencyProperty.Register(
            nameof(Wave),
            typeof(WaveLineCache),
            typeof(WaveCanvas),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                WaveChanged
            )
        );

        public WaveLineCache Wave
        {
            get { return (WaveLineCache)GetValue(WaveProperty); }
            set { SetValue(WaveProperty, value); }
        }

        protected override double PathHeight
        {
            get
            {
                return WaveLineCache.DefaultPathHeight;
            }
        }

        public WaveCanvas()
        {
            PenColor = System.Drawing.Color.FromArgb(255, 135, 203, 255);
        }

        protected override void UpdatePath(GraphicsPath path)
        {
            base.UpdatePath(path);

            if ((SampleCount - SampleRange.Begin) < 2)
            {
                return;
            }

            var samplesPerLine = SampleRange.Length / ActualWidth;
            var kvp = Wave.WaveLines.GetPair(samplesPerLine);

            switch(kvp.Value.Type)
            {
                case WaveLineType.PolyLine:
                    var showableSamples = SampleRange.Length;
                    path.AddLines(
                        kvp.Value.Line.Skip(SampleRange.Begin)
                            .TakeWhile((w, i) => i <= showableSamples)
                            .Select((w, i) => new PointF(i, w[0]))
                            .ToArray()
                    );
                    break;
                case WaveLineType.Bar:
                    var reductionCount = (int)kvp.Key;
                    var showableLines = SampleRange.Length / reductionCount + 1;
                    for (int i = SampleRange.Begin / reductionCount, c = 0; i < kvp.Value.Line.Length && c < showableLines; i++, c++)
                    {
                        path.AddLine(new PointF(c * reductionCount, kvp.Value.Line[i][0]), new PointF(c * reductionCount, kvp.Value.Line[i][1]));
                    }
                    break;
            }
        }

        static void WaveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((WaveCanvas)dependencyObject).SampleCount = ((WaveLineCache)e.NewValue)?.SampleCount ?? 0;
        }
    }
}
