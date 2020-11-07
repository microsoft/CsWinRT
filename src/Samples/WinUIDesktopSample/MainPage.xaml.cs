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
    public class LeakedObject : Page
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
            var val = Environment.GetEnvironmentVariable("CsWinRTNet5SdkVersion");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            static WeakReference MakeBaseWeakRef() => new WeakReference(new Page());

            static WeakReference MakeDerivedWeakRef() => new WeakReference(new LeakedObject());

            var baseRef = MakeBaseWeakRef();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var baseLeaked = baseRef.IsAlive ? "base leaked" : "base collected";

            var derivedRef = MakeDerivedWeakRef();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var derivedLeaked = derivedRef.IsAlive ? "derived leaked" : "derived collected";

            ((Button)sender).Content = baseLeaked + ", " + derivedLeaked;
        }
    }
}
