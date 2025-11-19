using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace ObjectLifetimeTests
{
    public sealed class CustomControl : ContentControl
    {
        static CustomControl()
        {
            // hack since CLR does not initialize static fields unless it is in a static ctor
            _ObjectProperty = DependencyProperty.Register("Object", typeof(Object), typeof(CustomControl), new PropertyMetadata(null, OnObjectChanged));
            _Object2Property = DependencyProperty.Register("Object2", typeof(Object), typeof(CustomControl), new PropertyMetadata(null, OnObject2Changed));
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

        // PropertyChanged callbacks -- forward to corresponding custom events.
        private static void OnObjectChanged(DependencyObject s, DependencyPropertyChangedEventArgs args)
        {
            CustomControl control = s as CustomControl;
            if (control != null && control.ObjectChanged != null)
                control.ObjectChanged(s, args);
        }

        private static void OnObject2Changed(DependencyObject s, DependencyPropertyChangedEventArgs args)
        {
            CustomControl control = s as CustomControl;
            if (control != null && control.Object2Changed != null)
                control.Object2Changed(s, args);
        }

        // Virtual override for Content property changed -- forward to corresponding custom event.
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (ContentChanged != null)
                ContentChanged(oldContent, newContent);
        }

        // Custom ObjectChanged event
        public event DependencyPropertyChangedEventHandler ObjectChanged;

        // Custom Object2Changed event
        public event DependencyPropertyChangedEventHandler Object2Changed;

        // Custom ContentChanged event
        public event EventHandler<object> ContentChanged;

        // Custom CLR event
        public event EventHandler<object> ClrEvent;

        public void RaiseClrEvent()
        {
            if (ClrEvent != null)
                ClrEvent(null, new EventArgs());
        }
    }
}