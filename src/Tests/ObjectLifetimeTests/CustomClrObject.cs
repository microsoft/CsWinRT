using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;


namespace ObjectLifetimeTests
{
    public sealed class CustomClrObject
    {
        // Hack since we dont yet support CLR properties in xaml. so, making it a DP instead.
        static CustomClrObject()
        {
            // hack since CLR does not initialize static fields unless it is in a static ctor
            _AttachedObjectProperty = DependencyProperty.RegisterAttached("AttachedObject", typeof(Object), typeof(CustomClrObject), null);
        }

        // Attached DependencyProperty
        private static readonly DependencyProperty _AttachedObjectProperty = null;
        public static DependencyProperty AttachedObjectProperty { get { return _AttachedObjectProperty; } }

        public static void SetAttachedObject(DependencyObject element, object value)
        {
            element.SetValue(_AttachedObjectProperty, value);
        }
        public static object GetAttachedObject(DependencyObject element)
        {
            return (object)element.GetValue(_AttachedObjectProperty);
        }

        // Clr Properties
        private object _object = 0;
        public object Object
        {
            get { return _object; }
            set { _object = value; }
        }

        private object _object2 = 0;
        public object Object2
        {
            get { return _object2; }
            set { _object2 = value; }
        }

        // Clr Event
        public event System.EventHandler<object> ClrEvent;

        public void RaiseClrEvent()
        {
            if (ClrEvent != null)
                ClrEvent(null, new EventArgs());
        }
    }
}