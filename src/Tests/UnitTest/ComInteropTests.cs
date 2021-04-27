using System;
using System.Runtime.InteropServices;
using Windows.Interop;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Xunit;

namespace UnitTest
{
    public class ComInteropTests
    {
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
            Assert.Throws<TypeInitializationException> (() => InputPaneInterop.GetForWindow(new IntPtr(0)));
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
        public void TestPrinting3DManager()
        {
            Assert.Throws<COMException>(() => Printing3DManagerInterop.GetForWindow(new IntPtr(0)));
            Assert.Throws<COMException>(() => Printing3DManagerInterop.ShowPrintUIForWindowAsync(new IntPtr(0)));

        }

        [Fact]
        public void TestRadialControllerConfiguration()
        {
            Assert.Throws<COMException>(() => RadialControllerConfigurationInterop.GetForWindow(new IntPtr(0)));
        }

        [Fact]
        public void TestRadialControllerIndependentInputSource()
        {
            var radialControllerIndependentInputSource =  RadialControllerIndependentInputSourceInterop.CreateForWindow(new IntPtr(0));
            Assert.IsType<Windows.UI.Input.Core.RadialControllerIndependentInputSource>(radialControllerIndependentInputSource);
        }

        [Fact]
        public void TestRadialControllerInterop()
        {
            var radialController = RadialControllerInterop.CreateForWindow(new IntPtr(0));
            Assert.IsType<Windows.UI.Input.RadialController>(radialController);

        }

        [Fact (Skip = "Compile-time only interop test")]
        public void TestSpatialInteractionManager()
        {
            Assert.Throws<COMException>(() => SpatialInteractionManagerInterop.GetForWindow(new IntPtr(0)));
        }

        [Fact (Skip = "Compile-time only interop test")]
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
        public async void TestWebAuthenticationCoreManager()
        {
            WebAccountProvider webAccountProvider = new WebAccountProvider("id", "name", null);
            WebTokenRequest webTokenRequest = new WebTokenRequest(webAccountProvider);
            Assert.Throws<ArgumentException>(() => WebAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(new IntPtr(0), null));

            var webAccount = new Windows.Security.Credentials.WebAccount(webAccountProvider, "user name", 0);
            Assert.Throws<ArgumentException>(() => WebAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(new IntPtr(0), webTokenRequest, webAccount));
        }
    }
}
