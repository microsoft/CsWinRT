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
