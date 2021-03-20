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
            }
        }

        private void Button2Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton2();
            var res = button.Tag;
        }


        private void Button3Click(object sender, RoutedEventArgs e)
        {
            var button = new CustomButton3();
            button.Click += (s, e) => button.Content = "Click";
        }

        private void Button4Click(object sender, RoutedEventArgs e)
        {
            var button = new Button();
            if (button != null)
            {
                num++;
            }
        }

        private void Button5Click(object sender, RoutedEventArgs e)
        {
            var button = new Button();
            button.Tag = 42;
        }

        private void Button6Click(object sender, RoutedEventArgs e)
        {
            var button = new Button();
            button.Click += (s, e) => button.Content = "Click";
        }

        private void GarbageCollect()
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
        }
    }

    public partial class CustomButton : Button
    {
        byte[] bytes = new byte[10_000_000];
    }

    public partial class CustomButton2 : Button
    {
        byte[] bytes = new byte[10_000_000];
    }

    public partial class CustomButton3 : Button
    {
        byte[] bytes = new byte[10_000_000];
    }
}
