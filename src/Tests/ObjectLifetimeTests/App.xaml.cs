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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            // We are doing workarounds to get this working with CsWinRT 3.0 given testhost extensions
            // has a version that has a 2.x dependency. Since we make it use the version of the extensions
            // without that dependency, that seems to cause issues where testhost's UnitTestClient.Run expects
            // to be launched passing --parentprocessid but it isn't.  So we detect and workaround that for now.
            // For now, always run the tests in-process, even when --parentprocessid is set.
            //if (Environment.CommandLine.Contains("--parentprocessid"))
            //{
            //    Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
            //}
            //else
            //{
                RunTestsInProcess();
            //}
        }

        // In-process runner for the standalone launch. The [TestMethod]s marshal work to the UI-thread
        // dispatcher and block on it, so they must run off the UI thread (which keeps pumping).
        private static void RunTestsInProcess()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                // Log via the framework Logger; under VSTest the test host captures OnLogMessage. In-process
                // there's no subscriber and a packaged Release app has no console, so route it to Trace.
                Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.LogMessageHandler onLogMessage =
                    message => System.Diagnostics.Trace.WriteLine(message);

                Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.OnLogMessage += onLogMessage;

                int passed = 0, failed = 0;

                foreach (System.Type type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute), false).Length == 0)
                    {
                        continue;
                    }

                    foreach (System.Reflection.MethodInfo method in type.GetMethods())
                    {
                        if (method.GetCustomAttributes(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute), false).Length == 0)
                        {
                            continue;
                        }

                        try
                        {
                            object instance = System.Activator.CreateInstance(type);
                            method.Invoke(instance, null);
                            passed++;
                            Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.LogMessage("PASS  {0}.{1}", type.Name, method.Name);
                        }
                        catch (System.Exception ex)
                        {
                            failed++;
                            Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.LogMessage("FAIL  {0}.{1}: {2}", type.Name, method.Name, (ex.InnerException ?? ex).Message);
                        }
                    }
                }

                Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.LogMessage("Summary: {0} passed, {1} failed.", passed, failed);

                Microsoft.VisualStudio.TestTools.UnitTesting.Logging.Logger.OnLogMessage -= onLogMessage;

                System.Environment.Exit(failed == 0 ? 0 : 1);
            });
        }

        public MainWindow m_window { get; set; }
    }
}