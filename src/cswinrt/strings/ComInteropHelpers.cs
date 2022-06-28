#if UAC_VERSION_15
#define UAC_VERSION_14
#endif
#if UAC_VERSION_14
#define UAC_VERSION_13
#endif
#if UAC_VERSION_13
#define UAC_VERSION_12
#endif
#if UAC_VERSION_12
#define UAC_VERSION_11
#endif
#if UAC_VERSION_11
#define UAC_VERSION_10
#endif
#if UAC_VERSION_10
#define UAC_VERSION_9
#endif
#if UAC_VERSION_9
#define UAC_VERSION_8
#endif
#if UAC_VERSION_8
#define UAC_VERSION_7
#endif
#if UAC_VERSION_7
#define UAC_VERSION_6
#endif
#if UAC_VERSION_6
#define UAC_VERSION_5
#endif
#if UAC_VERSION_5
#define UAC_VERSION_4
#endif
#if UAC_VERSION_4
#define UAC_VERSION_3
#endif
#if UAC_VERSION_3
#define UAC_VERSION_2
#endif
#if UAC_VERSION_2
#define UAC_VERSION_1
#endif
#if !UAC_VERSION_1
#error Unsupported Universal API Contract version
#endif

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Security.Credentials;
using WinRT;
using WinRT.Interop;

namespace WinRT.Interop
{
 
#if EMBED
    internal 
#else
    public
#endif
    static class IWindowNativeMethods
    {
        public static unsafe global::System.IntPtr get_WindowHandle(object _obj)
        {
            var asObjRef = global::WinRT.MarshalInspectable<object>.CreateMarshaler2(_obj, System.Guid.Parse("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB"));

            var ThisPtr = asObjRef.GetAbi();

            global::System.IntPtr __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out global::System.IntPtr, int>**)ThisPtr)[3](ThisPtr, out __retval));
            return __retval;
        }
    }


#if EMBED
    internal 
#else
    public
#endif
    static class WindowNative
    {
        public static IntPtr GetWindowHandle(object target) => IWindowNativeMethods.get_WindowHandle(target);   // target.As<IWindowNative>().WindowHandle;
    }
   
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    internal interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

#if EMBED
    internal 
#else
    public
#endif
    static class InitializeWithWindow
    {
        public static void Initialize(object target, IntPtr hwnd) => target.As<IInitializeWithWindow>().Initialize(hwnd);
    }
}

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class DragDropManagerInterop
    {
        private static IDragDropManagerInterop dragDropManagerInterop = CoreDragDropManager.As<IDragDropManagerInterop>();
        
        public static CoreDragDropManager GetForWindow(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(ICoreDragDropManager));
            return (CoreDragDropManager)dragDropManagerInterop.GetForWindow(appWindow, iid);
        }
    }
#endif
}

namespace Windows.Graphics.Printing
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class PrintManagerInterop
    {
        private static IPrintManagerInterop printManagerInterop = PrintManager.As<IPrintManagerInterop>();

        public static PrintManager GetForWindow(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IPrintManager));
            return (PrintManager)printManagerInterop.GetForWindow(appWindow, iid);
        }

        public static IAsyncOperation<bool> ShowPrintUIForWindowAsync(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IAsyncOperation<bool>));
            return (IAsyncOperation<bool>)printManagerInterop.ShowPrintUIForWindowAsync(appWindow, iid);
        }
    }
#endif
}

namespace Windows.Media
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class SystemMediaTransportControlsInterop
    {
        private static ISystemMediaTransportControlsInterop systemMediaTransportControlsInterop = SystemMediaTransportControls.As<ISystemMediaTransportControlsInterop>();

        public static SystemMediaTransportControls GetForWindow(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(ISystemMediaTransportControls));
            return (SystemMediaTransportControls)systemMediaTransportControlsInterop.GetForWindow(appWindow, iid);
        }
    }
#endif
}

namespace Windows.Media.PlayTo
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif 
    static class PlayToManagerInterop
    {
        private static IPlayToManagerInterop playToManagerInterop = PlayToManager.As<IPlayToManagerInterop>();
        
        public static PlayToManager GetForWindow(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IPlayToManager));
            return (PlayToManager)playToManagerInterop.GetForWindow(appWindow, iid);
        }

        public static void ShowPlayToUIForWindow(IntPtr appWindow)
        {
            playToManagerInterop.ShowPlayToUIForWindow(appWindow);
        }
    }
#endif
}

namespace Windows.Security.Credentials.UI
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class UserConsentVerifierInterop
    {
        private static IUserConsentVerifierInterop userConsentVerifierInterop = UserConsentVerifier.As<IUserConsentVerifierInterop>();

        public static IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(IntPtr appWindow, string message)
        {
            var iid = GuidGenerator.CreateIID(typeof(IAsyncOperation<UserConsentVerificationResult>));
            return (IAsyncOperation<UserConsentVerificationResult>)userConsentVerifierInterop.RequestVerificationForWindowAsync(appWindow, message, iid);
        }
    }
#endif
}

namespace Windows.Security.Authentication.Web.Core
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class WebAuthenticationCoreManagerInterop
    {
        private static IWebAuthenticationCoreManagerInterop webAuthenticationCoreManagerInterop = WebAuthenticationCoreManager.As<IWebAuthenticationCoreManagerInterop>();

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr appWindow, WebTokenRequest request)
        {
            var iid = GuidGenerator.CreateIID(typeof(IAsyncOperation<WebTokenRequestResult>));
            return (IAsyncOperation<WebTokenRequestResult>)webAuthenticationCoreManagerInterop.RequestTokenForWindowAsync(appWindow, request, iid);
        }

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(IntPtr appWindow, WebTokenRequest request, WebAccount webAccount)
        {
            var iid = GuidGenerator.CreateIID(typeof(IAsyncOperation<WebTokenRequestResult>));
            return (IAsyncOperation<WebTokenRequestResult>)webAuthenticationCoreManagerInterop.RequestTokenWithWebAccountForWindowAsync(appWindow, request, webAccount, iid);
        }
    }
#endif
}

namespace Windows.UI.ApplicationSettings
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class AccountsSettingsPaneInterop
    {
        private static IAccountsSettingsPaneInterop accountsSettingsPaneInterop = AccountsSettingsPane.As<IAccountsSettingsPaneInterop>();

        public static AccountsSettingsPane GetForWindow(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IAccountsSettingsPane));
            return (AccountsSettingsPane)accountsSettingsPaneInterop.GetForWindow(appWindow, iid);
        }

        public static IAsyncAction ShowManageAccountsForWindowAsync(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IAsyncAction));
            return (IAsyncAction)accountsSettingsPaneInterop.ShowManageAccountsForWindowAsync(appWindow, iid);
        }

        public static IAsyncAction ShowAddAccountForWindowAsync(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IAsyncAction));
            return (IAsyncAction)accountsSettingsPaneInterop.ShowAddAccountForWindowAsync(appWindow, iid);
        }
    }
#endif
}

namespace Windows.UI.Input
{
#if UAC_VERSION_3
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class RadialControllerConfigurationInterop
    {
        private static IRadialControllerConfigurationInterop radialControllerConfigurationInterop 
            = RadialControllerConfiguration.As<IRadialControllerConfigurationInterop>();
        
        public static RadialControllerConfiguration GetForWindow(IntPtr hwnd)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IRadialControllerConfiguration));
            return (RadialControllerConfiguration)radialControllerConfigurationInterop.GetForWindow(hwnd, iid);
        }
    }

#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class RadialControllerInterop
    {
        private static IRadialControllerInterop radialControllerInterop = RadialController.As<IRadialControllerInterop>();

        public static RadialController CreateForWindow(IntPtr hwnd)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IRadialController));
            return (RadialController)radialControllerInterop.CreateForWindow(hwnd, iid);
        }
    }
#endif
}

namespace Windows.UI.Input.Core
{
#if UAC_VERSION_4
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.15063.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class RadialControllerIndependentInputSourceInterop
    {
        private static IRadialControllerIndependentInputSourceInterop radialControllerIndependentInputSourceInterop 
            = RadialControllerIndependentInputSource.As<IRadialControllerIndependentInputSourceInterop>();

        public static RadialControllerIndependentInputSource CreateForWindow(IntPtr hwnd)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IRadialControllerIndependentInputSource));
            return (RadialControllerIndependentInputSource)radialControllerIndependentInputSourceInterop.CreateForWindow(hwnd, iid);
        }
    }
#endif
}

namespace Windows.UI.Input.Spatial
{
#if UAC_VERSION_2
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10586.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class SpatialInteractionManagerInterop
    {
        private static ISpatialInteractionManagerInterop spatialInteractionManagerInterop = SpatialInteractionManager.As<ISpatialInteractionManagerInterop>();
        
        public static SpatialInteractionManager GetForWindow(IntPtr window)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(ISpatialInteractionManager));
            return (SpatialInteractionManager)spatialInteractionManagerInterop.GetForWindow(window, iid);
        }
    }
#endif
}

namespace Windows.UI.ViewManagement
{
#if UAC_VERSION_1
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class InputPaneInterop
    {
        private static IInputPaneInterop inputPaneInterop = InputPane.As<IInputPaneInterop>();

        public static InputPane GetForWindow(IntPtr appWindow)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IInputPane));
            return (InputPane)inputPaneInterop.GetForWindow(appWindow, iid);
        }
    }

#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else
    public 
#endif
    static class UIViewSettingsInterop
    {
        private static IUIViewSettingsInterop uIViewSettingsInterop = UIViewSettings.As<IUIViewSettingsInterop>();

        public static UIViewSettings GetForWindow(IntPtr hwnd)
        {
            var iid = GuidGenerator.CreateIID(typeof(IUIViewSettings));
            return (UIViewSettings)uIViewSettingsInterop.GetForWindow(hwnd, iid);
        }
    }
#endif
}

namespace Windows.Graphics.Display
{
#if UAC_VERSION_15
#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.22621.0")]
#endif
#if EMBED
    internal
#else
    public
#endif
    static class DisplayInformationInterop
    {
        private static IDisplayInformationStaticsInterop displayInformationInterop = DisplayInformation.As<IDisplayInformationStaticsInterop>();

        public static DisplayInformation GetForWindow(IntPtr window)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IDisplayInformation));
            return (DisplayInformation)displayInformationInterop.GetForWindow(window, iid);
        }

        public static DisplayInformation GetForMonitor(IntPtr monitor)
        {
            Guid iid = GuidGenerator.CreateIID(typeof(IDisplayInformation));
            return (DisplayInformation)displayInformationInterop.GetForMonitor(monitor, iid);
        }
    }
#endif
}
