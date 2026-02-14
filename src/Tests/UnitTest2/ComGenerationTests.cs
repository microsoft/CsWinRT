using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Graphics.Display;
using Windows.Graphics.Printing;
using Windows.Media;
using Windows.Media.PlayTo;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Input;
using Windows.UI.Input.Core;
using Windows.UI.Input.Spatial;
using Windows.UI.ViewManagement;
using WinRT;
using TestComponentCSharp;

namespace UnitTest
{
    [GeneratedComInterface]
    [Guid("15651B9F-6C6B-4CC0-944C-C7D7B0F36F81")]
    internal partial interface IComInteropGenerated
    {
        long ReturnWindowHandle(IntPtr hwnd, Guid iid);
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
