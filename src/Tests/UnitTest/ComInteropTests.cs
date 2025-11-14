using System;
using System.Runtime.InteropServices;
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
using Xunit;
using TestComponentCSharp;
using System.Runtime.InteropServices.Marshalling;

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
    public class ComInteropTests
    {
        [Fact]
        public void TestHWND()
        {
            var comInterop = (IComInterop) Class.ComInterop;
            if (System.Environment.Is64BitProcess)
            {
                var hwnd = new IntPtr(0x0123456789ABCDEF);
                var value = comInterop.ReturnWindowHandle(hwnd, typeof(IComInterop).GUID);
                var hwndValue = hwnd.ToInt64();
                Assert.Equal(hwndValue, value);
            }
            else 
            {
                var hwnd = new IntPtr(0x01234567);
                var value = comInterop.ReturnWindowHandle(hwnd, typeof(IComInterop).GUID);
                var hwndValue = hwnd.ToInt32();
                Assert.Equal(hwndValue, value);
            }
        }

        [Fact]
        public void TestMockDragDropManager()
        {
            var interop = (WinRT.Interop.IDragDropManagerInterop) Class.ComInterop;
            Guid iid = typeof(ICoreDragDropManager).GUID;
            var manager = interop.GetForWindow(new IntPtr(0), iid);
            Assert.NotNull(manager);
        }

        [Fact]
        public void TestAccountsSettingsPane()
        {
            Assert.Throws<COMException>(() => AccountsSettingsPaneInterop.GetForWindow(new IntPtr(0)));
            Assert.Throws<COMException>(() => AccountsSettingsPaneInterop.ShowAddAccountForWindowAsync(new IntPtr(0)));
            Assert.Throws<COMException>(() => AccountsSettingsPaneInterop.ShowManageAccountsForWindowAsync(new IntPtr(0)));
        }

        [Fact]
        public void TestDragDropManager()
        {
            Assert.Throws<COMException>(() => DragDropManagerInterop.GetForWindow(new IntPtr(0)));
        }

        [Fact]
        public void TestInputPane()
        {
           Assert.Throws<TypeInitializationException>(() => InputPaneInterop.GetForWindow(new IntPtr(0)));
        }

        [Fact]
        public void TestPlayToManager()
        {
            Assert.Throws<COMException>(() => PlayToManagerInterop.GetForWindow(new IntPtr(0)));
            PlayToManagerInterop.ShowPlayToUIForWindow(new IntPtr(0));
        }

        [Fact]
        public void TestPrintManager()
        {
            Assert.Throws<COMException>(() => PrintManagerInterop.GetForWindow(new IntPtr(0)));
            Assert.Throws<COMException>(() => PrintManagerInterop.ShowPrintUIForWindowAsync(new IntPtr(0)));
        }

        [Fact]
        public void TestRadialControllerConfiguration()
        {
            Assert.Throws<COMException>(() => RadialControllerConfigurationInterop.GetForWindow(new IntPtr(0)));
        }

        // Skipping this test as it causes a hang 
        [Fact(Skip = "Compile-time only interop test")]
        public void TestRadialControllerIndependentInputSource()
        {
            var radialControllerIndependentInputSource = RadialControllerIndependentInputSourceInterop.CreateForWindow(new IntPtr(0));
            Assert.IsType<Windows.UI.Input.Core.RadialControllerIndependentInputSource>(radialControllerIndependentInputSource);
        }

        // Skipping this test as it causes a hang 
        [Fact(Skip = "Compile-time only interop test")]
        public void TestRadialControllerInterop()
        {
            var radialController = RadialControllerInterop.CreateForWindow(new IntPtr(0));
            Assert.IsType<Windows.UI.Input.RadialController>(radialController);

        }

        // Skipping this test as it raises non-catchable 'System.AccessViolationException' occurred in Windows.dll 
        [Fact(Skip = "Compile-time only interop test")]
        public void TestSpatialInteractionManager()
        {
           Assert.Throws<COMException>(() => SpatialInteractionManagerInterop.GetForWindow(new IntPtr(0)));
        }

        // Skipping this test as it raises non-catchable 'System.AccessViolationException' occurred in Windows.dll 
        [Fact(Skip = "Compile-time only interop test")]
        public void TestSystemMediaTransportControls()
        {
           Assert.Throws<COMException>(() => SystemMediaTransportControlsInterop.GetForWindow(new IntPtr(0)));
        }

        [Fact]
        public void TestUIViewSettings()
        {
           Assert.Throws<COMException>(() => UIViewSettingsInterop.GetForWindow(new IntPtr(0)));
        }

        [Fact]
        public void TestUserConsentVerifier()
        {
            var operation = UserConsentVerifierInterop.RequestVerificationForWindowAsync(new IntPtr(0), "message");
            Assert.NotNull(operation);
        }

        [Fact]
        public void TestWebAuthenticationCoreManager()
        {
            WebAccountProvider provider = new WebAccountProvider("id", "name", null);
            WebTokenRequest webTokenRequest = new WebTokenRequest(provider);
            Assert.Throws<ArgumentException>(() => WebAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(new IntPtr(0), webTokenRequest));
            var webAccount = new WebAccount(provider, "user name", 0);
            Assert.Throws<ArgumentException>(() => WebAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(new IntPtr(0), webTokenRequest, webAccount));
        }

        // Skipping as API isn't available in pipeline yet.
        [Fact(Skip = "Compile-time only interop test")]
        public void TestDisplayInformation()
        {
            Assert.Throws<COMException>(() => DisplayInformationInterop.GetForWindow(new IntPtr(0)));
            Assert.Throws<COMException>(() => DisplayInformationInterop.GetForMonitor(new IntPtr(0)));
        }

        [Fact]
        public void TestDataTransferManager()
        {
            Assert.Throws<COMException>(() => DataTransferManagerInterop.GetForWindow(new IntPtr(0)));
            Assert.Throws<COMException>(() => DataTransferManagerInterop.ShowShareUIForWindow(new IntPtr(0)));
        }
    }
}
