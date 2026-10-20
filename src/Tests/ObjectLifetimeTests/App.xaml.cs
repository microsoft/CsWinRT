using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ObjectLifetimeTests.Lifted
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>


        public App()
        {
            this.InitializeComponent();
        }

        // DISABLE_XAML_GENERATED_MAIN drops the XAML-generated Main (it calls the 2.x
        // WinRT.ComWrappersSupport.InitializeComWrappers(), which is gone in 3.0). Provide our own.
        [global::System.STAThread]
        static void Main(string[] args)
        {
            global::Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>        
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            Environment.ExitCode = 1;
            try
            {
                // AsyncQueue.Run blocks while dispatching UI work, so keep test execution off the UI thread.
                Environment.ExitCode = await System.Threading.Tasks.Task.Run(() =>
                    MicrosoftTestingPlatformApplication.RunAsync(Environment.GetCommandLineArgs()[1..]));
            }
            finally
            {
                m_window.Close();
                Exit();
            }
        }

        public MainWindow m_window { get; set; }
    }
}