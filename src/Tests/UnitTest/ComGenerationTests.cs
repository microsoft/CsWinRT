using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponentCSharp;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace UnitTest
{
    [GeneratedComInterface]
    [Guid("15651B9F-6C6B-4CC0-944C-C7D7B0F36F81")]
    internal partial interface IComInteropGenerated
    {
        Int64 ReturnWindowHandle(IntPtr hwnd, Guid iid);
    }

    [GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper, ExceptionToUnmanagedMarshaller = typeof(RestrictedErrorInfoExceptionMarshaller))]
    [Guid("09E1CDE3-76A5-4E01-B0EE-18D48860A55A")]
    internal partial interface IExceptionMarshalling
    {
        void Invoke();
    }

    // A second view of the same COM interface exposes its HRESULT as a marshalled exception.
    [GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
    [Guid("09E1CDE3-76A5-4E01-B0EE-18D48860A55A")]
    internal partial interface IExceptionMarshallingPreserveSig
    {
        [PreserveSig]
        [return: MarshalUsing(typeof(RestrictedErrorInfoExceptionMarshaller))]
        Exception Invoke();
    }

    [GeneratedComClass]
    internal sealed partial class ExceptionMarshalling(Exception exception) : IExceptionMarshalling
    {
        public void Invoke()
        {
            if (exception is not null)
            {
                throw exception;
            }
        }
    }

    [TestClass]
    public class ComGenerationTests
    {
        private static readonly Guid IID_IComInterop = new Guid("15651B9F-6C6B-4CC0-944C-C7D7B0F36F81");

        [TestMethod]
        public void TestHWND()
        {
            var comInterop = (IComInteropGenerated)(object)Class.ComInterop;

            if (Environment.Is64BitProcess)
            {
                var hwnd = new IntPtr(0x0123456789ABCDEF);
                var value = comInterop.ReturnWindowHandle(hwnd, IID_IComInterop);
                var hwndValue = hwnd.ToInt64();
                Assert.AreEqual(hwndValue, value);
            }
            else
            {
                var hwnd = new IntPtr(0x01234567);
                var value = comInterop.ReturnWindowHandle(hwnd, IID_IComInterop);
                var hwndValue = hwnd.ToInt32();
                Assert.AreEqual(hwndValue, value);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void TestRestrictedErrorInfoExceptionMarshaller_UnmanagedToManagedOut(bool throwException)
        {
            Exception expectedException = throwException ? new NotImplementedException("Generated COM exception") : null;
            var instance = new ExceptionMarshalling(expectedException);
            void* target = ComInterfaceMarshaller<IExceptionMarshalling>.ConvertToUnmanaged(instance);

            try
            {
                UnitTestHelper.RoClearError();

                int hresult = ((delegate* unmanaged[MemberFunction]<void*, int>)(*(void***)target)[3])(target);

                Assert.AreEqual(expectedException?.HResult ?? 0, hresult);
                Assert.AreSame(expectedException, RestrictedErrorInfo.GetExceptionForHR(hresult));
            }
            finally
            {
                ComInterfaceMarshaller<IExceptionMarshalling>.Free(target);
                UnitTestHelper.RoClearError();
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void TestRestrictedErrorInfoExceptionMarshaller_ManagedToUnmanagedOut(bool throwException)
        {
            Exception expectedException = throwException ? new NotImplementedException("Generated COM exception") : null;
            var instance = new ExceptionMarshalling(expectedException);
            void* target = ComInterfaceMarshaller<IExceptionMarshalling>.ConvertToUnmanaged(instance);

            try
            {
                // Force an RCW instead of unwrapping the CCW, so the generated native-call stub is exercised.
                object wrapper = UniqueComInterfaceMarshaller<IExceptionMarshallingPreserveSig>.ConvertToManaged(target);

                try
                {
                    UnitTestHelper.RoClearError();

                    Exception actualException = ((IExceptionMarshallingPreserveSig)wrapper).Invoke();

                    Assert.AreSame(expectedException, actualException);
                }
                finally
                {
                    ((ComObject)wrapper).FinalRelease();
                }
            }
            finally
            {
                ComInterfaceMarshaller<IExceptionMarshalling>.Free(target);
                UnitTestHelper.RoClearError();
            }
        }
    }
}
