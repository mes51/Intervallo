using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace Intervallo.UI.Behavior
{
    public class SampleScrollBehavior : Behavior<SampleRangeChangeableControl>, IDisposable
    {
        const int MinSampleCount = 5;

        public bool Disposed { get; private set; } = false;

        HwndSource NativeWindowSource { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (AssociatedObject.IsLoaded)
                {
                    Initialize();
                }
                else
                {
                    AssociatedObject.Loaded += AssociatedObject_Loaded;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.MouseWheel -= AssociatedObject_MouseWheel;
            Dispose();
        }

        void Initialize()
        {
            AssociatedObject.MouseWheel += AssociatedObject_MouseWheel;
            NativeWindowSource = HwndSource.FromHwnd(new WindowInteropHelper(Window.GetWindow(AssociatedObject)).Handle);
            NativeWindowSource.AddHook(WndProc);
        }

        void ScrollSample(int direction)
        {
            AssociatedObject.SampleRange = AssociatedObject.SampleRange.Move((int)Math.Ceiling(AssociatedObject.SampleRange.Length * 0.1) * Math.Sign(direction));
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEHWHEEL = 0x020E;

            var pos = Mouse.GetPosition(AssociatedObject);
            if (msg == WM_MOUSEHWHEEL && pos.X >= 0.0 && pos.X < AssociatedObject.ActualWidth && pos.Y >= 0.0 && pos.Y < AssociatedObject.ActualHeight)
            {
                var delta = wParam.ToInt32() >> 16;
                ScrollSample(delta);
            }

            return IntPtr.Zero;
        }

        void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }

        void AssociatedObject_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (AssociatedObject.SampleCount < 1)
            {
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                ScrollSample(-e.Delta);
            }
            else
            {
                var sampleRange = AssociatedObject.SampleRange;
                if (e.Delta > 0)
                {
                    var stretch = (int)Math.Ceiling((sampleRange.Length * 1.1)) - sampleRange.Length;
                    AssociatedObject.SampleRange = sampleRange.Stretch(stretch).Move(stretch / -2);
                }
                else
                {
                    var stretch = Math.Max((int)(sampleRange.Length * 0.9), MinSampleCount) - sampleRange.Length;
                    AssociatedObject.SampleRange = sampleRange.Stretch(stretch).Move(stretch / -2);
                }
            }
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                NativeWindowSource?.RemoveHook(WndProc);
                NativeWindowSource?.Dispose();
                Disposed = true;

                GC.SuppressFinalize(this);
            }
        }

        ~SampleScrollBehavior()
        {
            Dispose();
        }
    }
}
