using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        int num = 0;

        public MainPage()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        DispatcherTimer timer = new DispatcherTimer();
        private void Timer_Tick(object sender, object e)
        {
            GarbageCollect();
        }

        private void Button1Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton();
            if (button != null)
            {
                num++;
//                GarbageCollect();
            }
        }

        private void Button2Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton2();
            button.Tag = 42;

//            GarbageCollect();
        }


        private void Button3Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton3();
            button.Click += (s, e) => button.Content = "Click";

//            GarbageCollect();
        }

        private void GarbageCollect()
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }

    public partial class CustomButton : Button
    {
    }

    public partial class CustomButton2 : Button
    {
    }

    public partial class CustomButton3 : Button
    {
    }
}
