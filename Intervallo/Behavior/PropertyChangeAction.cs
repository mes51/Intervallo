using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace Intervallo.Behavior
{
    public class PropertySyncAction : TriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register(
            nameof(PropertyName),
            typeof(string),
            typeof(PropertySyncAction),
            new PropertyMetadata("")
        );

        public static readonly DependencyProperty SyncTargetProperty = DependencyProperty.Register(
            nameof(SyncTarget),
            typeof(DependencyObject),
            typeof(PropertySyncAction),
            new PropertyMetadata()
        );

        public string PropertyName
        {
            get { return (string)GetValue(PropertyNameProperty); }
            set { SetValue(PropertyNameProperty, value); }
        }

        public DependencyObject SyncTarget
        {
            get { return (DependencyObject)GetValue(SyncTargetProperty); }
            set { SetValue(SyncTargetProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            var srcProperty = AssociatedObject?.GetType()?.GetProperty(PropertyName);
            var dstProperty = SyncTarget?.GetType()?.GetProperty(PropertyName);
            if (srcProperty != null && dstProperty != null)
            {
                dstProperty.SetValue(SyncTarget, srcProperty.GetValue(AssociatedObject));
            }
        }
    }
}
