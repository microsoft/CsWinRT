using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using WinRT;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        LeakScenarios scenarios = new LeakScenarios();

        public MainPage()
        {
            this.InitializeComponent();
#if DEBUG
            Build.Text = "DEBUG";
#else
            Build.Text = "RELEASE";
#endif
        }

        private void Report(string status) => Status.Text = status;

        private void WithoutCapture_Click(object sender, RoutedEventArgs e) => scenarios.WithoutCapture_Click(Report);
        private void WithoutCapture_Check(object sender, RoutedEventArgs e) => scenarios.WithoutCapture_Check(Report);
        private void WithCapture_Click(object sender, RoutedEventArgs e) => scenarios.WithCapture_Click(Report);
        private void WithCapture_Check(object sender, RoutedEventArgs e) => scenarios.WithCapture_Check(Report);
        private void Alloc_Click(object sender, RoutedEventArgs e) => scenarios.Alloc_Click(Report);
        private void Check_Click(object sender, RoutedEventArgs e) => scenarios.Check_Click(Report);
    }
}
