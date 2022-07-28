using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Web.Http;
using Microsoft.System;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // NOTE: until microsoft.ui.xaml.dll is fixed, to enable unhandled error propagation,
            // set a BP in DXamlCore::InitializeInstance and force execution of AddRefRegistration().
            UnhandledException += (sender, e) =>
            {
                var ex = e.Exception;
                var stackTrace = ex.StackTrace;
                if (global::System.Diagnostics.Debugger.IsAttached) global::System.Diagnostics.Debugger.Break();
            };
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            myWindow.Content = new MainPage();
        }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            global::Microsoft.UI.Xaml.Application.Start((p) => {
                // TODO: fix DispatcherQueueSynchronizationContext in IXP projection 
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });

            Microsoft.UI.Xaml.Application.Start((e) => new App());
        }
    }
}
