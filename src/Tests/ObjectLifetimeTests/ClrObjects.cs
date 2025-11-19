using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;


namespace ObjectLifetimeTests
{
    public class ClrObject /* hack to get DP support */: DependencyObject
    {
        public ClrObject() { IntProp = 0; }

        public int IntProp
        {
            get
            {
                return (Int32)this.GetValue(IntPropProperty);
            }

            set
            {
                this.SetValue(IntPropProperty, value);
            }
        }

        public override string ToString()
        {
            return "I am a Clr object: " + IntProp.ToString();

        }

        // Hack since we dont yet support CLR properties in xaml. so, making it a DP instead.
        static ClrObject()
        {
            // hack since CLR does not initialize static fields unless it is in a static ctor
            IntPropProperty = DependencyProperty.Register("IntProp", typeof(int), typeof(ClrObject), null);
        }

        //public static readonly DependencyProperty IntPropProperty = DependencyProperty.Register("IntProp", typeof(int), typeof(JupiterSUnitDrts.CoreServices.ClrObject), null);
        public static readonly DependencyProperty IntPropProperty = null;

    }

    public class ClrObject2 : System.ComponentModel.INotifyPropertyChanged
    {
        private int _intProp = 0;
        public int IntProperty
        {
            get { return _intProp; }
            set
            {
                _intProp = value;
                OnPropertyChanged("IntProperty");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ClrObject3
    {
        private object _myTag = 0;
        public object MyTag
        {
            get { return _myTag; }
            set
            {
                _myTag = value;
            }
        }
    }
}