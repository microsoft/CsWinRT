using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUIDesktopSample
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            this.Foo = "bar";
        }

        static DependencyProperty FooProperty = DependencyProperty.Register("Foo", typeof(string), typeof(MainPage), null);

        public string Foo
        {
            get => (string)GetValue(FooProperty);
            set => SetValue(FooProperty, value);
        }
    }
}
