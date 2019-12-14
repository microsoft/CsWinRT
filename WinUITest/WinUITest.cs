// WinUI projection smoke test (compile only)
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

        [Fact]
        public void TestSomeWinUI()
        {
            WinrtModule module = new WinrtModule();

            Microsoft.UI.Xaml.Controls.Frame frame = new Microsoft.UI.Xaml.Controls.Frame();

            // TODO: load up some MUX!
            //Assert.Equal(true, true);
        }
    }
}
