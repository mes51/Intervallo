﻿using System;
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
                PenChanged
            )
        );

        public static readonly DependencyProperty PenWidthProperty = DependencyProperty.Register(
            nameof(PenWidth),
            typeof(double),
            typeof(GDILineDrawableControl),
            new FrameworkPropertyMetadata(
                1.0,
                FrameworkPropertyMetadataOptions.AffectsRender,
                PenChanged
            )
        );

        public Color PenColor
        {
            get { return (Color)GetValue(PenColorProperty); }
            set { SetValue(PenColorProperty, value); }
        }

        public double PenWidth
        {
            get { return (double)GetValue(PenWidthProperty); }
            set { SetValue(PenWidthProperty, value); }
        }

        protected virtual double PathHeight { get; }

        protected virtual double BaseY { get; }

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

            if (Path.PointCount < 2)
            {
                return;
            }

            var progress = (float)GetSampleProgress();
            var heightScale = (float)(ActualHeight / PathHeight);

            using (Matrix m = new Matrix())
            {
                m.Scale(progress, heightScale);
                m.Translate(0.0F, (float)BaseY);
                g.Transform = m.Clone();
                m.Invert();
                Pen.Transform = m;
                g.DrawPath(Pen, Path);

                if (progress > 10.0F)
                {
                    var pointSize = Math.Min(10.0F, progress / 10.0F);
                    using (var points = new GraphicsPath(FillMode.Winding))
                    using (var brush = new SolidBrush(Pen.Color))
                    {
                        foreach (var p in Path.PathPoints)
                        {
                            points.AddRectangle(new RectangleF(p.X - pointSize * 0.5F / progress, p.Y - pointSize * 0.5F / heightScale, pointSize / progress, pointSize / heightScale));
                        }
                        g.FillPath(brush, points);
                    }
                }
            }
        }

        protected void RefreshPath()
        {
            Path.Reset();
            UpdatePath(Path);
        }

        protected virtual double GetSampleProgress()
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

        static void PenChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = dependencyObject as GDILineDrawableControl;
            control.Pen.Color = control.PenColor;
            control.Pen.Width = (float)control.PenWidth;
        }
    }
}
