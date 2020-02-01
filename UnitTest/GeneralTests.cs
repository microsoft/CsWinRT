using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinRT;

using WF = Windows.Foundation;
using WFC = Windows.Foundation.Collections;
using Windows.Foundation;
using Windows.Foundation.Collections;

using TestComponent;

namespace UnitTest
{
    public class TestComponent
    {
        public TestComponent()
        {
        }

        [Fact]
        public void RunTests()
        {
            var percentage = TestRunner.TestCaller((ITests tests) => {
                // todo... implement all ITests, including delegate callbacks to exercise native-managed marshaling
                //tests.ArrayParams_Bool(new bool[] { true, false, true }, );
            });
            //Assert.Equal((double)percentage, (double)100);
            Assert.Equal((double)percentage, (double)0);
        }
    }
}
