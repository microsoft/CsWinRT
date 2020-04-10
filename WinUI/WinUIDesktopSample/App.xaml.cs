using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var window = new Microsoft.UI.Xaml.Window
            {
                // This will cause the InvalidCast exception. Once that is fixed, this can
                // be removed and just use the new MainPage() line below for setting content
                Content = new Microsoft.UI.Xaml.Shapes.Rectangle()
                {
                    Fill = new SolidColorBrush(Windows.UI.Colors.Red),
                    Width = 200,
                    Height = 200
                }
            };

            window.Content = new MainPage();

            window.Activate();
        }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Microsoft.UI.Xaml.Application.Start((e) => new App());
        }
    }
}
