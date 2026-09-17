using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;

namespace UnitTest
{
    // The 'IRestrictedErrorInfo' object of a thread is ambient, sticky state: it is set as a side effect of
    // some previous failure on that same thread (ie. any failing native call), and CsWinRT deliberately puts
    // it back after reading it, so that it also outlives the call that observed it. 'GetExceptionForHR' uses
    // it to enrich (or outright restore) the exception it produces, but it may only do so when the error info
    // actually belongs to the 'HRESULT' being converted. Associating an unrelated error object with a new
    // exception is not just cosmetic: 'GetHRForException' reads the 'HRESULT' back out of the attached error
    // object, so such an exception would later marshal out as an entirely different error code.
    [TestClass]
    public class ExceptionTests
    {
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const int RPC_E_WRONG_THREAD = unchecked((int)0x8001010E);
        private const int ERROR_INVALID_WINDOW_HANDLE = unchecked((int)0x80070578);

        private const string MatchingErrorMessage = "Matching originated error";
        private const string UnrelatedErrorMessage = "Unrelated originated error";

        [TestMethod]
        public void TestGetExceptionForHR_WithValidHResult_ReturnsSystemFormattedException()
        {
            UnitTestHelper.RoClearError();

            Exception exception = RestrictedErrorInfo.GetExceptionForHR(RPC_E_WRONG_THREAD);
            Assert.IsNotNull(exception);
            Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Message));

            if (CultureInfo.CurrentUICulture.Name == "en-US")
            {
                Assert.AreEqual("The application called an interface that was marshalled for a different thread. (0x8001010E)", exception.Message);
            }
        }

        [TestMethod]
        public void TestGetExceptionForHR_WithoutRestrictedErrorInfo_MapsHResultOnly()
        {
            UnitTestHelper.RoClearError();

            Exception exception = RestrictedErrorInfo.GetExceptionForHR(E_NOTIMPL);

            Assert.IsInstanceOfType<NotImplementedException>(exception);
            Assert.AreEqual(E_NOTIMPL, exception.HResult);
            Assert.IsNull(exception.Data["__RestrictedErrorObjectReference"]);
            Assert.AreEqual(E_NOTIMPL, RestrictedErrorInfo.GetHRForException(exception));
        }

        [TestMethod]
        public void TestGetExceptionForHR_WithMatchingRestrictedErrorInfo_AssociatesErrorInfo()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(E_NOTIMPL, MatchingErrorMessage));

                Exception exception = RestrictedErrorInfo.GetExceptionForHR(E_NOTIMPL);

                Assert.IsInstanceOfType<NotImplementedException>(exception);
                Assert.AreEqual(E_NOTIMPL, exception.HResult);

                // The error info does belong to this 'HRESULT', so all its details flow into the exception
                Assert.Contains(MatchingErrorMessage, exception.Message);
                Assert.AreEqual(MatchingErrorMessage, exception.Data["RestrictedDescription"]);
                Assert.IsNotNull(exception.Data["__RestrictedErrorObjectReference"]);

                // Associating the error info must still round-trip the original 'HRESULT'
                Assert.AreEqual(E_NOTIMPL, RestrictedErrorInfo.GetHRForException(exception));
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        public void TestGetExceptionForHR_WithMismatchedRestrictedErrorInfo_IgnoresErrorInfo()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(ERROR_INVALID_WINDOW_HANDLE, UnrelatedErrorMessage));

                Exception exception = RestrictedErrorInfo.GetExceptionForHR(E_NOTIMPL);

                Assert.IsInstanceOfType<NotImplementedException>(exception);
                Assert.AreEqual(E_NOTIMPL, exception.HResult);

                // None of the details of the unrelated error info may leak into the exception
                Assert.DoesNotContain(UnrelatedErrorMessage, exception.Message);
                Assert.IsNull(exception.Data["Description"]);
                Assert.IsNull(exception.Data["RestrictedDescription"]);
                Assert.IsNull(exception.Data["__RestrictedErrorObjectReference"]);

                // Most importantly, marshalling the exception back out must produce the 'HRESULT' it was
                // created for, and not the one carried by the unrelated error info of the current thread
                Assert.AreEqual(E_NOTIMPL, RestrictedErrorInfo.GetHRForException(exception));
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        public void TestGetExceptionForHR_WithMismatchedRestrictedErrorInfo_PreservesThreadErrorInfo()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(ERROR_INVALID_WINDOW_HANDLE, UnrelatedErrorMessage));

                // Ignoring the error info must not consume it: it is only ever borrowed, so it stays on the
                // thread for whichever call it actually belongs to (ie. an 'HRESULT' ABI return value)
                _ = RestrictedErrorInfo.GetExceptionForHR(E_NOTIMPL);

                Exception exception = RestrictedErrorInfo.GetExceptionForHR(ERROR_INVALID_WINDOW_HANDLE);

                Assert.AreEqual(UnrelatedErrorMessage, exception.Data["RestrictedDescription"]);
                Assert.IsNotNull(exception.Data["__RestrictedErrorObjectReference"]);
                Assert.AreEqual(ERROR_INVALID_WINDOW_HANDLE, RestrictedErrorInfo.GetHRForException(exception));
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        public void TestGetHRForException_WithMismatchedRestrictedErrorInfo_ReturnsExceptionHResult()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(ERROR_INVALID_WINDOW_HANDLE, UnrelatedErrorMessage));

                Assert.AreEqual(E_NOTIMPL, RestrictedErrorInfo.GetHRForException(new NotImplementedException()));
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        public void TestGetHRForException_AfterGetExceptionForHRWithMismatchedRestrictedErrorInfo_RoundTripsHResult()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(ERROR_INVALID_WINDOW_HANDLE, UnrelatedErrorMessage));

                // This is exactly the round-trip a 'Windows.Foundation.HResult' does when it is marshalled
                // into managed code by a CCW stub and then marshalled back out again (eg. a property setter
                // followed by its getter). The 'HRESULT' has to survive it completely unchanged.
                Exception exception = RestrictedErrorInfo.GetExceptionForHR(E_NOTIMPL);

                Assert.AreEqual(E_NOTIMPL, RestrictedErrorInfo.GetHRForException(exception));
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        public void TestThrowExceptionForHR_WithMismatchedRestrictedErrorInfo_ThrowsMappedException()
        {
            try
            {
                Assert.IsTrue(UnitTestHelper.OriginateError(ERROR_INVALID_WINDOW_HANDLE, UnrelatedErrorMessage));

                NotImplementedException exception = Assert.ThrowsExactly<NotImplementedException>(
                    () => RestrictedErrorInfo.ThrowExceptionForHR(E_NOTIMPL));

                Assert.AreEqual(E_NOTIMPL, exception.HResult);
                Assert.DoesNotContain(UnrelatedErrorMessage, exception.Message);
            }
            finally
            {
                UnitTestHelper.RoClearError();
            }
        }
    }
}