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
            LogStartup("App constructor entered");
            UnhandledException += (_, e) => LogStartup($"XAML unhandled exception: {e.Exception}");
            LogStartup("App.InitializeComponent starting");
            this.InitializeComponent();
            LogStartup("App.InitializeComponent completed");
        }

        // DISABLE_XAML_GENERATED_MAIN drops the XAML-generated Main (it calls the 2.x
        // WinRT.ComWrappersSupport.InitializeComWrappers(), which is gone in 3.0). Provide our own.
        [global::System.STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                LogStartup($"Managed unhandled exception (terminating={e.IsTerminating}): {e.ExceptionObject}");
            LogStartup($"Main entered; arguments={string.Join(" ", args)}");
            LogStartup("Application.Start starting");
            global::Microsoft.UI.Xaml.Application.Start((p) =>
            {
                LogStartup("Application.Start callback entered");
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                LogStartup("Creating App");
                _ = new App();
                LogStartup("App created");
            });
            LogStartup($"Application.Start returned; exit code={Environment.ExitCode}");
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>        
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            LogStartup("OnLaunched entered; creating MainWindow");
            m_window = new MainWindow();
            LogStartup("MainWindow created; activating");
            m_window.Activate();
            LogStartup("MainWindow activated");

            Environment.ExitCode = 1;
            try
            {
                // AsyncQueue.Run blocks while dispatching UI work, so keep test execution off the UI thread.
                Environment.ExitCode = await System.Threading.Tasks.Task.Run(async () =>
                {
                    LogStartup("MTP RunAsync starting");
                    int exitCode = await MicrosoftTestingPlatformApplication.RunAsync(Environment.GetCommandLineArgs()[1..]);
                    LogStartup($"MTP RunAsync completed; exit code={exitCode}");
                    return exitCode;
                });
            }
            finally
            {
                LogStartup("Closing MainWindow");
                m_window.Close();
                LogStartup("Exiting Application");
                Exit();
            }
        }

        private static readonly object s_startupLogLock = new();

        private static void LogStartup(string message)
        {
            string directory = Environment.GetEnvironmentVariable("TESTINGPLATFORM_DIAGNOSTIC_OUTPUT_DIRECTORY");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                lock (s_startupLogLock)
                {
                    File.AppendAllText(System.IO.Path.Combine(directory, $"startup-{Environment.ProcessId}.log"),
                        $"{DateTimeOffset.UtcNow:O} [thread {Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}");
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not write startup diagnostics: {e}");
            }
        }

        public MainWindow m_window { get; set; }
    }
}