using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Threading;

namespace Intervallo.UI.Behavior
{
    public class SizeChangeBehavior : Behavior<FrameworkElement>
    {
        public event EventHandler SizeChanged;

        DispatcherTimer Timer { get; } = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 16), IsEnabled = true };

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
            AssociatedObject.Unloaded += AssociatedObject_Unloaded;

            var prevSize = new { Width = AssociatedObject.ActualWidth, Height = AssociatedObject.ActualHeight };
            Timer.Tick += (sender, e) =>
            {
                if (AssociatedObject != null && prevSize.Width != AssociatedObject.ActualWidth || prevSize.Height != AssociatedObject.ActualHeight)
                {
                    prevSize = new { Width = AssociatedObject.ActualWidth, Height = AssociatedObject.ActualHeight };
                    OnSizeChanged();
                }
            };
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            Unload();
        }

        void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }

        void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OnSizeChanged();
        }

        void Unload()
        {
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
            AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
            Timer.Stop();
        }

        void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
