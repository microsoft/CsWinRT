using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using WinRT;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnitTest
{
    public class TestWinUI
    {
        public TestWinUI()
        {
        }

        public class App : Microsoft.UI.Xaml.Application
        {

        }

        // Compile time test to ensure multiple allowed attributes 
        [TemplatePart(Name = "PartButton", Type = typeof(Button))]
        // [TemplatePart(Name = "PartGrid", Type = typeof(Grid))]
        public class TestAllowMultipleAttributes { };

        [Fact]
        public void TestApp()
        {
            WinrtModule module = new WinrtModule();

            var app = new App();
            // TODO: load up some MUX!
            //Assert.Equal(true, true);
        }
    }
}
