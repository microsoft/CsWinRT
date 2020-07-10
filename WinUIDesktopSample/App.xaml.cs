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
        }

        Window myWindow;
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var value = DependencyProperty.UnsetValue;
            var button = new Button
            {
                Content = "Click me to load MainPage",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            button.Click += Button_Click;
            var window = new Microsoft.UI.Xaml.Window
            {
                Content = button
            };

            window.Activate();

            myWindow = window;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            const string _root = "https://api.github.com";
            const string _repoName = "WindowsCommunityToolkit";
            const string _repoOwner = "Microsoft";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko");

                var uri = $"{_root}/repos/{_repoOwner}/{_repoName}/releases";
                var result = await client.GetStringAsync(new Uri(uri));
            }
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
