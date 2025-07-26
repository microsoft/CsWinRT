using System;
using System.Globalization;
using WinRT;
using Xunit;

namespace UnitTest
{
    public class ExceptionTests
    {
        [Fact]
        public void TestGetExceptionForHR_WithValidHResult_ReturnsSystemFormattedException()
        {
            const int RPC_E_WRONG_THREAD = unchecked((int)0x8001010E);

            Exception exception = ExceptionHelpers.GetExceptionForHR(RPC_E_WRONG_THREAD);
            Assert.NotNull(exception);
            Assert.False(string.IsNullOrWhiteSpace(exception.Message));
            if (CultureInfo.CurrentUICulture.Name == "en-US")
            {
                Assert.Equal("The application called an interface that was marshalled for a different thread. (0x8001010E)", exception.Message);
            }
        }
    }
}
