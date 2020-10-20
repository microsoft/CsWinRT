using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinRT;
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
    //public sealed partial class FirstPageHandler
    //{

    //    public static void Button_Click(object sender, RoutedEventArgs e)
    //    {
    //        App.Navigate(typeof(FirstPage));
    //    }
    //}

    class TestPage : Page, IWinRTObject
    {
        public TestPage()
        {
        }
        //bool IWinRTObject.HasUnwrappableNativeObject => true;
        //IObjectReference IWinRTObject.NativeObject => base.NativeObject; // TODO: Doesn't work, but what I need?
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstPage : Page
    {
        public FirstPage()
            // : base(((IWinRTObject)new Page()).NativeObject) // Was a thought, but doesn't work.
        {
            this.InitializeComponent();
            MyButton.Click += Button_Click;

        }

        private static void Button_Click(object sender, RoutedEventArgs e)
        {
            App.Navigate(typeof(FirstPage));
            //{
            //    var testPage = new TestPage();
            //    var page = new Page();

            //    Console.WriteLine();
            //}
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
