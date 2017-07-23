using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Intervallo.UI.Shape
{
    public class Arc : UserControl
    {
        public static readonly DependencyProperty StartAngleProperty = DependencyProperty.Register(
            nameof(StartAngle),
            typeof(double),
            typeof(Arc),
            new FrameworkPropertyMetadata(
                0.0,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty FinishAngleProperty = DependencyProperty.Register(
            nameof(FinishAngle),
            typeof(double),
            typeof(Arc),
            new FrameworkPropertyMetadata(
                360.0,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(Arc),
            new FrameworkPropertyMetadata(
                Brushes.Black,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(Arc),
            new FrameworkPropertyMetadata(
                1.0,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(Arc),
            new FrameworkPropertyMetadata(
                Brushes.Black,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public double FinishAngle
        {
            get { return (double)GetValue(FinishAngleProperty); }
            set { SetValue(FinishAngleProperty, value); }
        }

        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        Pen Pen { get; } = new Pen(Brushes.Black, 1.0);

        protected override void OnRender(DrawingContext drawingContext)
        {
            var strokeThickness = StrokeThickness;
            if (Stroke == null || double.IsNaN(strokeThickness))
            {
                strokeThickness = 0.0;
            }

            var startAngle = RoundAngle(StartAngle);
            var finishAngle = RoundAngle(FinishAngle);
            var path = new PathGeometry();

            if (Math.Abs(startAngle - finishAngle) == 360.0)
            {
                path.AddGeometry(new EllipseGeometry(new Rect(strokeThickness * 0.5, strokeThickness * 0.5, ActualWidth - strokeThickness, ActualHeight - strokeThickness)));
            }
            else
            {
                strokeThickness *= 0.5;
                var hasStroke = strokeThickness != 0.0;
                var size = new Size(ActualWidth * 0.5 - strokeThickness, ActualHeight * 0.5 - strokeThickness);
                var sp = AngleToPoint(Math.Min(startAngle, finishAngle) + 270.0, size.Width, size.Height, strokeThickness);
                var fp = AngleToPoint(Math.Max(startAngle, finishAngle) + 270.0, size.Width, size.Height, strokeThickness);
                var figure = new PathFigure();
                figure.StartPoint = new Point(ActualWidth * 0.5, ActualHeight * 0.5);
                figure.Segments.Add(new LineSegment(sp, false));
                figure.Segments.Add(new ArcSegment(fp, size, 0.0, Math.Abs(startAngle - finishAngle) >= 180.0, SweepDirection.Clockwise, hasStroke));
                figure.Segments.Add(new LineSegment(new Point(ActualWidth * 0.5, ActualHeight * 0.5), false));
                path.Figures.Add(figure);
            }

            Pen.Brush = Stroke;
            Pen.Thickness = StrokeThickness;
            drawingContext.DrawGeometry(Fill, Pen, path);
        }

        static Point AngleToPoint(double angle, double width, double height, double strokeThickness)
        {
            var rad = Math.PI / 180.0 * angle;
            var pos = new Point(width + width * Math.Cos(rad), height + height * Math.Sin(rad));
            pos.Offset(strokeThickness, strokeThickness);
            return pos;
        }

        static double RoundAngle(double angle)
        {
            while (angle > 360.0)
            {
                angle -= 360.0;
            }
            while (angle < -360.0)
            {
                angle += 360.0;
            }
            return angle;
        }
    }
}
