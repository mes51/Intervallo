using Intervallo.Audio;
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
            typeof(WaveCache),
            typeof(WaveCanvas),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                WaveChanged
            )
        );

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

        protected override double PathHeight
        {
            get
            {
                return WaveLineMap.DefaultPathHeight;
            }
        }

        WaveLineMap LineMap { get; set; }

        public WaveCanvas()
        {
            PenColor = System.Drawing.Color.FromArgb(255, 74, 171, 246);
        }

        protected override void UpdatePath(GraphicsPath path)
        {
            base.UpdatePath(path);

            if ((SampleCount - SampleRange.Begin) < 2)
            {
                return;
            }

            var samplesPerLine = SampleRange.Length / ActualWidth;
            var kvp = LineMap.WaveLines.GetPair(samplesPerLine);

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
            var waveCanvas = dependencyObject as WaveCanvas;
            waveCanvas.LineMap = new WaveLineMap(waveCanvas.Wave.Wave);
            waveCanvas.RefreshPath();
            waveCanvas.RedrawBitmap();
        }
    }
}
