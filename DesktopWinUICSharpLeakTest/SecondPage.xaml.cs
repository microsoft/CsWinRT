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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SecondPage : Page
    {
        public SecondPage()
        {
            this.InitializeComponent();

            //this.TheListView.ItemsSource = GetItems();
        }

        //public IEnumerable<string> GetItems()
        //{
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        yield return "Item " + (i + 1);
        //    }
        //}

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    App.Navigate(typeof(FirstPage));
        //}
    }
}
