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
        long ReturnWindowHandle(IntPtr hwnd, Guid iid);
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
        public void TestMockDragDropManager()
        {
            var interop = (WinRT.Interop.IDragDropManagerInterop)Class.ComInterop;
            Guid iid_ICoreDragDropManager = new("7D56D344-8464-4FAF-AA49-37EA6E2D7BD1");
            var manager = interop.GetForWindow(new IntPtr(0), iid_ICoreDragDropManager);
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void TestAccountsSettingsPane()
        {
            Assert.ThrowsException<COMException>(() => AccountsSettingsPaneInterop.GetForWindow(new IntPtr(0)));
            Assert.ThrowsException<COMException>(() => AccountsSettingsPaneInterop.ShowAddAccountForWindowAsync(new IntPtr(0)));
            Assert.ThrowsException<COMException>(() => AccountsSettingsPaneInterop.ShowManageAccountsForWindowAsync(new IntPtr(0)));
        }

        [TestMethod]
        public void TestDragDropManager()
        {
            Assert.ThrowsException<COMException>(() => DragDropManagerInterop.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestInputPane()
        {
            Assert.ThrowsException<TypeInitializationException>(() => InputPaneInterop.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestPlayToManager()
        {
            Assert.ThrowsException<COMException>(() => PlayToManagerInterop.GetForWindow(new IntPtr(0)));
            PlayToManagerInterop.ShowPlayToUIForWindow(new IntPtr(0));
        }

        [TestMethod]
        public void TestPrintManager()
        {
            Assert.ThrowsException<COMException>(() => PrintManagerInterop.GetForWindow(new IntPtr(0)));
            Assert.ThrowsException<COMException>(() => PrintManagerInterop.ShowPrintUIForWindowAsync(new IntPtr(0)));
        }

        [TestMethod]
        public void TestRadialControllerConfiguration()
        {
            Assert.ThrowsException<COMException>(() => RadialControllerConfigurationInterop.GetForWindow(new IntPtr(0)));
        }

        // Skipping this test as it causes a hang
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestRadialControllerIndependentInputSource()
        {
            var radialControllerIndependentInputSource =
                RadialControllerIndependentInputSourceInterop.CreateForWindow(new IntPtr(0));

            Assert.IsInstanceOfType<Windows.UI.Input.Core.RadialControllerIndependentInputSource>(
                radialControllerIndependentInputSource);
        }

        // Skipping this test as it causes a hang
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestRadialControllerInterop()
        {
            var radialController = RadialControllerInterop.CreateForWindow(new IntPtr(0));
            Assert.IsInstanceOfType<Windows.UI.Input.RadialController>(radialController);
        }

        // Skipping this test as it raises non-catchable 'System.AccessViolationException' occurred in Windows.dll
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestSpatialInteractionManager()
        {
            Assert.ThrowsException<COMException>(() => SpatialInteractionManagerInterop.GetForWindow(new IntPtr(0)));
        }

        // Skipping this test as it raises non-catchable 'System.AccessViolationException' occurred in Windows.dll
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestSystemMediaTransportControls()
        {
            Assert.ThrowsException<COMException>(() => SystemMediaTransportControlsInterop.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestUIViewSettings()
        {
            Assert.ThrowsException<COMException>(() => UIViewSettingsInterop.GetForWindow(new IntPtr(0)));
        }

        [TestMethod]
        public void TestUserConsentVerifier()
        {
            var operation = UserConsentVerifierInterop.RequestVerificationForWindowAsync(new IntPtr(0), "message");
            Assert.IsNotNull(operation);
        }

        [TestMethod]
        public void TestWebAuthenticationCoreManager()
        {
            WebAccountProvider provider = new WebAccountProvider("id", "name", null);
            WebTokenRequest webTokenRequest = new WebTokenRequest(provider);

            Assert.ThrowsException<ArgumentException>(() =>
                WebAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(new IntPtr(0), webTokenRequest));

            var webAccount = new WebAccount(provider, "user name", 0);

            Assert.ThrowsException<ArgumentException>(() =>
                WebAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(
                    new IntPtr(0), webTokenRequest, webAccount));
        }

        // Skipping as API isn't available in pipeline yet.
        [TestMethod]
        [Ignore("Compile-time only interop test")]
        public void TestDisplayInformation()
        {
            Assert.ThrowsException<COMException>(() => DisplayInformationInterop.GetForWindow(new IntPtr(0)));
            Assert.ThrowsException<COMException>(() => DisplayInformationInterop.GetForMonitor(new IntPtr(0)));
        }

        [TestMethod]
        public void TestDataTransferManager()
        {
            Assert.ThrowsException<COMException>(() => DataTransferManagerInterop.GetForWindow(new IntPtr(0)));
            Assert.ThrowsException<COMException>(() => DataTransferManagerInterop.ShowShareUIForWindow(new IntPtr(0)));
        }
    }
}