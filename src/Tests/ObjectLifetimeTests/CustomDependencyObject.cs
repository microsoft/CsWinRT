using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;


namespace ObjectLifetimeTests
{
    public sealed class CustomDependencyObject : DependencyObject
    {
        static CustomDependencyObject()
        {
            // hack since CLR does not initialize static fields unless it is in a static ctor
            _ObjectProperty = DependencyProperty.Register("Object", typeof(Object), typeof(CustomDependencyObject), null);
            _Object2Property = DependencyProperty.Register("Object2", typeof(Object), typeof(CustomDependencyObject), null);
            _AttachedObjectProperty = DependencyProperty.RegisterAttached("AttachedObject", typeof(Object), typeof(CustomDependencyObject), null);
        }

        // Custom DependencyProperties
        private static readonly DependencyProperty _ObjectProperty = null;
        public static DependencyProperty ObjectProperty { get { return _ObjectProperty; } }
        public Object Object
        {
            get { return GetValue(_ObjectProperty); }
            set { SetValue(_ObjectProperty, value); }
        }

        private static readonly DependencyProperty _Object2Property = null;
        public static DependencyProperty Object2Property { get { return _Object2Property; } }
        public Object Object2
        {
            get { return GetValue(_Object2Property); }
            set { SetValue(_Object2Property, value); }
        }

        // Custom Attached DependencyProperty
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
    }
}