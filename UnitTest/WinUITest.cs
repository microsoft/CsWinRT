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

namespace WinUITest
{
    public class TestProjection
    {
        public TestProjection()
        {
        }

        public class TestApp : Microsoft.UI.Xaml.Application
        {

        }

        [Fact]
        public void TestSomeWinUI()
        {
            WinrtModule module = new WinrtModule();

            var app = new TestApp();
            // TODO: load up some MUX!
            //Assert.Equal(true, true);
        }
    }
}
