#if NET8_0_OR_GREATER
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using ComServerHelpers;
using ComServerHelpers.CsWinRT;
using Windows.Foundation;
using Xunit;

namespace UnitTest
{
    public class ComServerHelperTests
    {
        [Fact]
        public void TestCOMRegistrationAndActivation()
        {
            ComServer server = new ComServer();
            OOPAsyncAction obj = new OOPAsyncAction();
            server.RegisterClass<OOPAsyncAction, IAsyncAction>(() => { return obj; });
            server.Start();

            var currentExecutingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var launchExePath = $"{currentExecutingDir}\\OOPExe.exe";
            var proc = Process.Start(launchExePath);

            Thread.Sleep(5000);
            obj.Close();
            Assert.True(obj.delegateCalled);
            server.Dispose();
        }
    }
}
#endif
