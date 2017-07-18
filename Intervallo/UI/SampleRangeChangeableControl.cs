using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Intervallo.UI
{
    public class SampleRangeChangeableControl : UserControl
    {
        public static readonly DependencyProperty SampleRangeProperty = DependencyProperty.Register(
            nameof(SampleRange),
            typeof(IntRange),
            typeof(SampleRangeChangeableControl),
            new FrameworkPropertyMetadata(
                new IntRange(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                SampleRangeChanged
            )
        );

        public IntRange SampleRange
        {
            get
            {
                return (IntRange)GetValue(SampleRangeProperty);
            }
            set
            {
                SetValue(SampleRangeProperty, value.Adjust(0.To(SampleCount)));
            }
        }

        public int ScrollableSampleCount
        {
            get
            {
                return Math.Max(0, SampleCount - SampleRange.Length);
            }
        }

        public virtual int SampleCount { get; }

        protected virtual void OnSampleRangeChanged() { }

        static void SampleRangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            (dependencyObject as SampleRangeChangeableControl).OnSampleRangeChanged();
        }
    }
}
