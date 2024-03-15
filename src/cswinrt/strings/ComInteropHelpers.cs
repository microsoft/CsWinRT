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
    internal static class IWindowNativeMethods
    {
        internal static readonly Guid IWindowNativeIID = new(0xEECDBF0E, 0xBAE9, 0x4CB6, 0xA6, 0x8E, 0x95, 0x98, 0xE1, 0xCB, 0x57, 0xBB);

        public static unsafe global::System.IntPtr get_WindowHandle(object _obj)
        {
            var asObjRef = global::WinRT.MarshalInspectable<object>.CreateMarshaler2(_obj, IWindowNativeIID);
            var ThisPtr = asObjRef.GetAbi();
            global::System.IntPtr __retval = default;

            try
            {
                // We use 3 here because IWindowNative only implements IUnknown, whose only functions are AddRef, Release and QI
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>**)ThisPtr)[3](ThisPtr, out __retval));
                return __retval;
            }
            finally
            {
                asObjRef.Dispose();
            }
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class WindowNative
    {
        public static IntPtr GetWindowHandle(object target) => IWindowNativeMethods.get_WindowHandle(target);
    }

    internal static class IInitializeWithWindowMethods
    {
        internal static readonly Guid IInitializeWithWindowIID = new(0x3E68D4BD, 0x7135, 0x4D10, 0x80, 0x18, 0x9F, 0xB6, 0xD9, 0xF3, 0x3F, 0xA1);

        public static unsafe void Initialize(object _obj, IntPtr window)
        {
            var asObjRef = global::WinRT.MarshalInspectable<object>.CreateMarshaler2(_obj, IInitializeWithWindowIID);
            var ThisPtr = asObjRef.GetAbi();
            global::System.IntPtr __retval = default;

            try
            {
                // IInitializeWithWindow inherits IUnknown (3 functions) and provides Initialize giving a total of 4 functions
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)ThisPtr)[3](ThisPtr, window));
            }
            finally
            {
                asObjRef.Dispose();
            }
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class InitializeWithWindow
    {
        public static void Initialize(object target, IntPtr hwnd)
        {
            IInitializeWithWindowMethods.Initialize(target, hwnd);
        }
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

namespace Windows.ApplicationModel.DataTransfer
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
    static class DataTransferManagerInterop
    {
        private static readonly global::System.Guid riid = new global::System.Guid(0xA5CAEE9B, 0x8708, 0x49D1, 0x8D, 0x36, 0x67, 0xD2, 0x5A, 0x8D, 0xA0, 0x0C);

#if NET
        private static global::WinRT.IObjectReference objectReference = global::WinRT.ActivationFactory.Get("Windows.ApplicationModel.DataTransfer.DataTransferManager", new global::System.Guid(0x3A3DCD6C, 0x3EAB, 0x43DC, 0xBC, 0xDE, 0x45, 0x67, 0x1C, 0xE8, 0x00, 0xC8));
#else
        private static global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> objectReference = global::WinRT.ActivationFactory.Get<global::WinRT.Interop.IUnknownVftbl>("Windows.ApplicationModel.DataTransfer.DataTransferManager", new global::System.Guid(0x3A3DCD6C, 0x3EAB, 0x43DC, 0xBC, 0xDE, 0x45, 0x67, 0x1C, 0xE8, 0x00, 0xC8));
#endif

        public static global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(global::System.IntPtr appWindow)
        {
            return IDataTransferManagerInteropMethods.GetForWindow(objectReference, appWindow, riid);
        }

        public static void ShowShareUIForWindow(global::System.IntPtr appWindow)
        {
            IDataTransferManagerInteropMethods.ShowShareUIForWindow(objectReference, appWindow);
        }
    }
#endif

    internal static class IDataTransferManagerInteropMethods
    {
        internal static unsafe global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(global::WinRT.IObjectReference _obj, global::System.IntPtr appWindow, in global::System.Guid riid)
        {
            global::System.IntPtr thisPtr = _obj.ThisPtr;
            global::System.IntPtr ptr = default;


            try
            {
                // IDataTransferManagerInterop inherits IUnknown (3 functions) and provides GetForWindow giving a total of 5 functions
                fixed(Guid* _riid = &riid)
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<global::System.IntPtr, global::System.IntPtr, global::System.Guid*, global::System.IntPtr*, int>**)thisPtr)[3](thisPtr, appWindow, _riid, &ptr));
                return global::WinRT.MarshalInspectable<global::Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(ptr);
            }
            finally
            {
                global::WinRT.MarshalInspectable<DataTransferManager>.DisposeAbi(ptr);
            }
        }

        internal static unsafe void ShowShareUIForWindow(global::WinRT.IObjectReference _obj, global::System.IntPtr appWindow)
        {
            global::System.IntPtr thisPtr = _obj.ThisPtr;

            // IDataTransferManagerInterop inherits IUnknown (3 functions) and provides ShowShareUIForWindow giving a total of 5 functions
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<global::System.IntPtr, global::System.IntPtr, int>**)thisPtr)[4](thisPtr, appWindow));
        }
    }
}
