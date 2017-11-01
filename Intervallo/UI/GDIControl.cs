using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Intervallo.UI
{
    public class GDIControl : SampleRangeChangeableControl, IDisposable
    {
        public bool Disposed { get; set; } = false;

        int DataLength { get; set; } = 4;

        WriteableBitmap Bitmap { get; set; } = new WriteableBitmap(1, 1, 96.0, 96.0, PixelFormats.Bgra32, null);

        Bitmap NativeBitmap { get; set; } = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        DispatcherTimer Timer { get; } = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 16), IsEnabled = true };

        public GDIControl()
        {
            var prevSize = new { Width = ActualWidth, Height = ActualHeight };
            var prevRange = SampleRange;
            Timer.Tick += (sender, e) =>
            {
                if (prevSize.Width != ActualWidth || prevSize.Height != ActualHeight)
                {
                    var intWidth = Math.Max((int)Math.Ceiling(ActualWidth), 1);
                    var intHeight = Math.Max((int)Math.Ceiling(ActualHeight), 1);
                    Bitmap = new WriteableBitmap(intWidth, intHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
                    NativeBitmap = new System.Drawing.Bitmap(intWidth, intHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    DataLength = intWidth * intHeight * 4;
                    prevSize = new { Width = ActualWidth, Height = ActualHeight };
                    if (!Disposed)
                    {
                        if (prevRange != SampleRange)
                        {
                            prevRange = SampleRange;
                            OnBitmapSizeChanged();
                        }
                        RedrawBitmap();
                        InvalidateVisual();
                    }
                }
            };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, ActualWidth, ActualHeight)));
            drawingContext.DrawImage(Bitmap, new Rect(0.0, 0.0, Bitmap.Width, Bitmap.Height));
            drawingContext.Pop();
        }

        protected virtual void OnBitmapSizeChanged() { }

        protected virtual void Draw(Graphics g) { }

        protected void RedrawBitmap()
        {
            using (Graphics g = Graphics.FromImage(NativeBitmap))
            {
                g.Clear(System.Drawing.Color.Transparent);

                if (ActualWidth > 0.0 && ActualHeight > 0.0)
                {
                    Draw(g);
                }
            }

            var bitmapLock = NativeBitmap.LockBits(new Rectangle(new System.Drawing.Point(), NativeBitmap.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Bitmap.WritePixels(new Int32Rect(0, 0, NativeBitmap.Width, NativeBitmap.Height), bitmapLock.Scan0, DataLength, NativeBitmap.Width * 4);
            NativeBitmap.UnlockBits(bitmapLock);
        }

        public virtual void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Timer.Stop();
            NativeBitmap.Dispose();

            GC.SuppressFinalize(this);
        }

        ~GDIControl()
        {
            Dispose();
        }
    }
}
