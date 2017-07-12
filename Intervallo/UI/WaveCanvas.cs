using Intervallo.Audio.Filter;
using Intervallo.Util;
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
    public partial class WaveCanvas : UserControl, IDisposable
    {
        public static readonly DependencyProperty WaveProperty = DependencyProperty.Register(
            nameof(Wave),
            typeof(double[]),
            typeof(WaveCanvas),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                WaveChanged
            )
        );

        public static readonly DependencyProperty SampleRangeProperty = DependencyProperty.Register(
            nameof(SampleRange),
            typeof(Range),
            typeof(WaveCanvas),
            new FrameworkPropertyMetadata(
                new Range(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                VisibleSampleChanged
            )
        );

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

        WaveLineMap LineMap { get; set; }

        bool Disposed { get; set; } = false;

        DispatcherTimer Timer { get; } = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 16), IsEnabled = true };

        int DataLength { get; set; } = 4;

        WriteableBitmap Bitmap { get; set; } = new WriteableBitmap(1, 1, 96.0, 96.0, PixelFormats.Bgra32, null);

        Bitmap NativeBitmap { get; set; } = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        GraphicsPath Path { get; set; } = new GraphicsPath();

        System.Drawing.Pen WavePen { get; } = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 43, 137, 201));

        public WaveCanvas()
        {
            var prevSize = new { Width = ActualWidth, Height = ActualHeight };
            var prevRange = SampleRange;
            Timer.Tick += (sender, e) =>
            {
                if (prevSize.Width != ActualWidth || prevSize.Height != ActualHeight)
                {
                    var intWidth = (int)Math.Ceiling(ActualWidth);
                    var intHeight = (int)Math.Ceiling(ActualHeight);
                    Bitmap = new WriteableBitmap(intWidth, intHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
                    NativeBitmap = new System.Drawing.Bitmap(intWidth, intHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    DataLength = intWidth * intHeight * 4;
                    prevSize = new { Width = ActualWidth, Height = ActualHeight };
                    if (prevRange != SampleRange)
                    {
                        prevRange = SampleRange;
                        RefreshPath();
                    }
                    RedrawBitmap();
                    InvalidateVisual();
                }
            };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawImage(Bitmap, new Rect(0.0, 0.0, Bitmap.Width, Bitmap.Height));
        }

        double GetSampleProgress()
        {
            return ActualWidth / Math.Max(1, SampleRange.Length);
        }

        void RefreshPath()
        {
            if (Disposed || (WaveSampleCount - SampleRange.Begin) < 2)
            {
                return;
            }

            Path.Reset();

            var samplesPerLine = SampleRange.Length / ActualWidth;
            var kvp = LineMap.WaveLines.GetPair(samplesPerLine);

            switch(kvp.Value.Type)
            {
                case WaveLineType.PolyLine:
                    var showableSamples = SampleRange.Length;
                    Path.AddLines(
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
                        Path.AddLine(new PointF(c * reductionCount, kvp.Value.Line[i][0]), new PointF(c * reductionCount, kvp.Value.Line[i][1]));
                    }
                    break;
            }
        }

        void RedrawBitmap()
        {
            using (Graphics g = Graphics.FromImage(NativeBitmap))
            using (System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix())
            {
                g.Clear(System.Drawing.Color.Transparent);

                if (ActualWidth > 0.0 && ActualHeight > 0.0 && Path.PointCount > 0)
                {
                    var progress = (float)GetSampleProgress();
                    m.Scale(progress, (float)(ActualHeight / WaveLineMap.DefaultPathHeight));
                    g.Transform = m.Clone();
                    m.Invert();
                    WavePen.Transform = m;
                    g.DrawPath(WavePen, Path);
                }
            }

            var bitmapLock = NativeBitmap.LockBits(new Rectangle(new System.Drawing.Point(), NativeBitmap.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Bitmap.WritePixels(new Int32Rect(0, 0, NativeBitmap.Width, NativeBitmap.Height), bitmapLock.Scan0, DataLength, NativeBitmap.Width * 4);
            NativeBitmap.UnlockBits(bitmapLock);
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Timer.Stop();
            WavePen.Dispose();
            Path.Dispose();
            NativeBitmap.Dispose();
        }

        static void WaveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var waveCanvas = dependencyObject as WaveCanvas;
            waveCanvas.LineMap = new WaveLineMap(waveCanvas.Wave);
            waveCanvas.RefreshPath();
            waveCanvas.RedrawBitmap();
        }

        static void VisibleSampleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var waveCanvas = dependencyObject as WaveCanvas;
            waveCanvas.RefreshPath();
            waveCanvas.RedrawBitmap();
        }
    }
}
