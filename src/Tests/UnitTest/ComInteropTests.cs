using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;
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
using TestComponentCSharp;

namespace UnitTest
{
    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("15651B9F-6C6B-4CC0-944C-C7D7B0F36F81")]
    internal partial interface IComInterop
    {
        Int64 ReturnWindowHandle(IntPtr hwnd, Guid iid);
    }

    // Note: Many of the COM interop APIs cannot be easily tested without significant test setup.
    // These cases either expect a runtime exception, or are compile-time only (skipped to validate types).
    [TestClass]
    public class ComInteropTests
    {
        [TestMethod]
        public void TestHWND()
        {
            var comInterop = (IComInterop)Class.ComInterop;
            if (Environment.Is64BitProcess)
            {
                var hwnd = new IntPtr(0x0123456789ABCDEF);
                var value = comInterop.ReturnWindowHandle(hwnd, typeof(IComInterop).GUID);
                var hwndValue = hwnd.ToInt64();
                Assert.AreEqual(hwndValue, value);
            }
            else
            {
                var hwnd = new IntPtr(0x01234567);
                var value = comInterop.ReturnWindowHandle(hwnd, typeof(IComInterop).GUID);
                var hwndValue = hwnd.ToInt32();
                Assert.AreEqual(hwndValue, value);
            }
        }

        [TestMethod]
        public void TestAccountsSettingsPane()
        {
            Assert.ThrowsExactly<COMException>(() => AccountsSettingsPane.GetForWindow(new IntPtr(0)));
            Assert.ThrowsExactly<COMException>(() => AccountsSettingsPane.ShowAddAccountForWindowAsync(new IntPtr(0)));
            Assert.ThrowsExactly<COMException>(() => AccountsSettingsPane.ShowManageAccountsForWindowAsync(new IntPtr(0)));
        }

        [TestMethod]
        public void TestDragDropManager()
        {
            Assert.ThrowsExactly<COMException>(() => CoreDragDropManager.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestInputPane()
        {
            Assert.ThrowsExactly<TypeInitializationException>(() => InputPane.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestPlayToManager()
        {
            Assert.ThrowsExactly<COMException>(() => PlayToManager.GetForWindow(new IntPtr(0)));
            PlayToManager.ShowPlayToUIForWindow(new IntPtr(0));
        }

        [TestMethod]
        public void TestPrintManager()
        {
            Assert.ThrowsExactly<COMException>(() => PrintManager.GetForWindow(new IntPtr(0)));
            Assert.ThrowsExactly<COMException>(() => PrintManager.ShowPrintUIForWindowAsync(new IntPtr(0)));
        }

        [TestMethod]
        public void TestRadialControllerConfiguration()
        {
            Assert.ThrowsExactly<COMException>(() => RadialControllerConfiguration.GetForWindow(new IntPtr(0)));
        }

        // Skipping this test as it causes a hang
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestRadialControllerIndependentInputSource()
        {
            var radialControllerIndependentInputSource =
                RadialControllerIndependentInputSource.CreateForWindow(new IntPtr(0));

            Assert.IsInstanceOfType<Windows.UI.Input.Core.RadialControllerIndependentInputSource>(
                radialControllerIndependentInputSource);
        }

        // Skipping this test as it causes a hang
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestRadialControllerInterop()
        {
            var radialController = RadialController.CreateForWindow(new IntPtr(0));
            Assert.IsInstanceOfType<Windows.UI.Input.RadialController>(radialController);
        }

        // Skipping this test as it raises non-catchable 'System.AccessViolationException' occurred in Windows.dll
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestSpatialInteractionManager()
        {
            Assert.ThrowsExactly<COMException>(() => SpatialInteractionManager.GetForWindow(new IntPtr(0)));
        }

        // Skipping this test as it raises non-catchable 'System.AccessViolationException' occurred in Windows.dll
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestSystemMediaTransportControls()
        {
            Assert.ThrowsExactly<COMException>(() => SystemMediaTransportControls.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestUIViewSettings()
        {
            Assert.ThrowsExactly<COMException>(() => UIViewSettings.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestUserConsentVerifier()
        {
            var operation = UserConsentVerifier.RequestVerificationForWindowAsync(new IntPtr(0), "message");
            Assert.IsNotNull(operation);
        }

        [TestMethod]
        public void TestWebAuthenticationCoreManager()
        {
            WebAccountProvider provider = new WebAccountProvider("id", "name", null);
            WebTokenRequest webTokenRequest = new WebTokenRequest(provider);

            Assert.ThrowsExactly<ArgumentException>(() =>
                WebAuthenticationCoreManager.RequestTokenForWindowAsync(new IntPtr(0), webTokenRequest));

            var webAccount = new WebAccount(provider, "user name", 0);

            Assert.ThrowsExactly<ArgumentException>(() =>
                WebAuthenticationCoreManager.RequestTokenWithWebAccountForWindowAsync(
                    new IntPtr(0), webTokenRequest, webAccount));
        }

        // Skipping as API isn't available in pipeline yet.
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestDisplayInformation()
        {
            Assert.ThrowsExactly<COMException>(() => DisplayInformation.GetForWindow(new IntPtr(0)));
            Assert.ThrowsExactly<COMException>(() => DisplayInformation.GetForMonitor(new IntPtr(0)));
        }

        [TestMethod]
        public void TestDataTransferManager()
        {
            Assert.ThrowsExactly<COMException>(() => DataTransferManager.GetForWindow(new IntPtr(0)));
            Assert.ThrowsExactly<COMException>(() => DataTransferManager.ShowShareUIForWindow(new IntPtr(0)));
        }
    }
}