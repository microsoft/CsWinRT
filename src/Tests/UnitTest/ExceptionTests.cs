using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;

namespace UnitTest
{
    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        public void TestGetExceptionForHR_WithValidHResult_ReturnsSystemFormattedException()
        {
            const int RPC_E_WRONG_THREAD = unchecked((int)0x8001010E);

            Exception exception = RestrictedErrorInfo.GetExceptionForHR(RPC_E_WRONG_THREAD);
            Assert.IsNotNull(exception);
            Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Message));

            if (CultureInfo.CurrentUICulture.Name == "en-US")
            {
                Assert.AreEqual(
                    "The application called an interface that was marshalled for a different thread. (0x8001010E)",
                    exception.Message);
            }
        }
    }
}