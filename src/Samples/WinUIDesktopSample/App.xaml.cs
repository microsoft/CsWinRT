using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel;


namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Window myWindow;
        
        public App()
        {
            WinRT.ComWrappersSupport.RegisterProjectionAssembly(typeof(App).Assembly);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        public static void Navigate(Type pageType)
        {
            App app = (App)Current;
            MainWindow mainWindow = (MainWindow)app.myWindow;
            mainWindow.Frame.Navigate(pageType);
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            myWindow = new MainWindow();
            myWindow.Activate();
        }


        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // Save application state and stop any background activity
        }
    }

    /*    
    public static class Program
    {
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            Microsoft.UI.Xaml.Application.Start((e) => new App());
        }
    }*/
}
