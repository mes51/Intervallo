using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Intervallo.UI
{
    public class GDILineDrawableControl : GDIControl
    {
        public static readonly DependencyProperty PenColorProperty = DependencyProperty.Register(
            nameof(PenColor),
            typeof(Color),
            typeof(GDILineDrawableControl),
            new FrameworkPropertyMetadata(
                Color.Black,
                FrameworkPropertyMetadataOptions.AffectsRender,
                PenColorChanged
            )
        );

        public Color PenColor
        {
            get { return (Color)GetValue(PenColorProperty); }
            set { SetValue(PenColorProperty, value); }
        }

        protected virtual double PathHeight { get; }

        GraphicsPath Path { get; set; } = new GraphicsPath();

        Pen Pen { get; } = new Pen(Color.FromArgb(255, 43, 137, 201));

        protected virtual void UpdatePath(GraphicsPath path) { }

        protected override void OnSampleRangeChanged()
        {
            base.OnSampleRangeChanged();

            RefreshPath();
            RedrawBitmap();
        }

        protected override void OnBitmapSizeChanged()
        {
            base.OnBitmapSizeChanged();

            RefreshPath();
        }

        protected override void Draw(Graphics g)
        {
            base.Draw(g);

            using (Matrix m = new Matrix())
            {
                if (Path.PointCount > 1)
                {
                    var progress = (float)GetSampleProgress();
                    m.Scale(progress, (float)(ActualHeight / PathHeight));
                    g.Transform = m.Clone();
                    m.Invert();
                    Pen.Transform = m;
                    g.DrawPath(Pen, Path);
                }
            }
        }

        protected void RefreshPath()
        {
            Path.Reset();
            UpdatePath(Path);
        }

        double GetSampleProgress()
        {
            return ActualWidth / Math.Max(1, SampleRange.Length);
        }

        public override void Dispose()
        {
            if (!Disposed)
            {
                Pen.Dispose();
                Path.Dispose();
            }

            base.Dispose();
        }

        static void PenColorChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = dependencyObject as GDILineDrawableControl;
            control.Pen.Color = control.PenColor;
        }
    }
}
