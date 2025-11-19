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
    }
}