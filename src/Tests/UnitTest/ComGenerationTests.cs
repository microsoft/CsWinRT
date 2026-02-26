using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using TestComponentCSharp;

namespace UnitTest
{
    [GeneratedComInterface]
    [Guid("15651B9F-6C6B-4CC0-944C-C7D7B0F36F81")]
    internal partial interface IComInteropGenerated
    {
        Int64 ReturnWindowHandle(IntPtr hwnd, Guid iid);
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
    }
}
