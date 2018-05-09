using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Intervallo.UI
{
    class CachedSampleRangeLine : SampleRangeChangeableControl
    {
        public static readonly DependencyProperty PreviewableSampleRangesProperty = DependencyProperty.Register(
            nameof(PreviewableSampleRanges),
            typeof(IReadOnlyList<IntRange>),
            typeof(CachedSampleRangeLine),
            new FrameworkPropertyMetadata(
                new List<IntRange>(),
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange
            )
        );

        public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
            nameof(LineBrush),
            typeof(Brush),
            typeof(CachedSampleRangeLine),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Color.FromArgb(255, 60, 255, 60)),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public IReadOnlyList<IntRange> PreviewableSampleRanges
        {
            get { return (IReadOnlyList<IntRange>)GetValue(PreviewableSampleRangesProperty); }
            set { SetValue(PreviewableSampleRangesProperty, value); }
        }

        public Brush LineBrush
        {
            get { return (Brush)GetValue(LineBrushProperty); }
            set { SetValue(LineBrushProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if ((PreviewableSampleRanges?.Count ?? 0) > 0)
            {
                var widthPerSample = ActualWidth / SampleRange.Length;

                foreach (var sample in PreviewableSampleRanges.Where(SampleRange.IsOverlap))
                {
                    drawingContext.DrawRectangle(LineBrush, null, new Rect((sample.Begin - SampleRange.Begin) * widthPerSample, 0.0, sample.Length * widthPerSample, ActualHeight));
                }
            }
        }

        protected override void OnSampleRangeChanged()
        {
            base.OnSampleRangeChanged();

            InvalidateVisual();
        }
    }
}
