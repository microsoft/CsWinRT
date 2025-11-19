using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;


namespace ObjectLifetimeTests
{
    public class CycleTestCanvas : Canvas
    {
        public CycleTestCanvas()
        {
        }

        public void OnPointerEntered(object sender, PointerRoutedEventArgs args)
        {
        }

        public static readonly DependencyProperty DP1Property
            = DependencyProperty.RegisterAttached("DP1", typeof(Control), typeof(CycleTestCanvas), null);

        public static readonly DependencyProperty DP2Property
            = DependencyProperty.RegisterAttached("DP2", typeof(Control), typeof(CycleTestCanvas), null);
    }
}