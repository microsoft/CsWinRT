using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace WinUIDesktopSample
{
    public class Derived : Grid
    {
        byte[] bytes = new byte[10_000_000];
    };

    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private WeakReference baseRef = new WeakReference(new Grid());
        private WeakReference derivedRef = new WeakReference(new Derived());
        private List<object> pressure = new List<object>();

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            pressure.Add(new byte[10_000_000]);
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            var baseStatus = baseRef.IsAlive ? "base leaked" : "base collected";
            var derivedStatus = derivedRef.IsAlive ? "derived leaked" : "derived collected";
            Status.Text = baseStatus + ", " + derivedStatus;
        }
    }
}
