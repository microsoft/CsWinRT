using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            this.AddHandler(UIElement.TappedEvent, new TappedEventHandler(Foo_PointerTapped), true /*handledEventsToo*/);
        }

        private void Foo_PointerTapped(object sender, TappedRoutedEventArgs e)
        {
        }

        static private int ThrowException()
        {
            throw new InvalidOperationException("Can't touch this");
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";
            // Test error propagation on UI thread
            ThrowException();
            // Test error propagation on thread pool
            int t = await Task.Run(ThrowException);
        }
    }
}
