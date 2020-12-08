using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DesktopWinUICSharpLeakTest
{
    public class LeakedObject : Page
    {
        byte[] bytes = new byte[10_000_000];
    };
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstPage : Page
    {
        public FirstPage()
        {
            this.InitializeComponent();

            this.TheListView.ItemsSource = GetItems();
        }

        public IEnumerable<string> GetItems()
        {
            for (int i = 0; i < 1000; i++)
            {
                yield return "Item " + (i + 1);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            static WeakReference MakeBaseWeakRef() => new WeakReference(new Page());

            static WeakReference MakeDerivedWeakRef() => new WeakReference(new LeakedObject());

            var baseRef = MakeBaseWeakRef();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            bool baseLeaked = baseRef.IsAlive;

            var derivedRef = MakeDerivedWeakRef();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            bool derivedLeaked = derivedRef.IsAlive;

            ((Button)sender).Content = derivedLeaked ? "object leaked" : "object collected";

            //App.Navigate(typeof(SecondPage));
        }
    }
}
