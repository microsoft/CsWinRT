using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Web.Http;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }

        Window myWindow;
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            myWindow = new Window();
            myWindow.Activate();
            myWindow.Content = new MainPage();
        }
    }

/*    public static class Program
    {
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Microsoft.UI.Xaml.Application.Start((e) => new App());
        }
    }*/
}
