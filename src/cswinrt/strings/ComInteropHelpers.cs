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

#pragma warning disable CSWINRT3001

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Security.Credentials;
using WinRT;
using WinRT.Interop;

namespace WinRT.Interop
{
    internal static class IWindowNativeMethods
    {
        private static ref readonly Guid IID_IWindowNative
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x0E, 0xBF, 0xCD, 0xEE, 0xE9, 0xBA, 0xB6, 0x4C, 0xA6, 0x8E, 0x95, 0x98, 0xE1, 0xCB, 0x57, 0xBB
                ];

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static unsafe global::System.IntPtr get_WindowHandle(object _obj)
        {
            using global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue objRefValue = WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeInterfaceMarshaller<object>.ConvertToUnmanaged(_obj, IID_IWindowNative);

            void* thisPtr = objRefValue.GetThisPtrUnsafe();

            global::System.IntPtr __retval = default;
            // We use 3 here because IWindowNative only implements IUnknown, whose only functions are AddRef, Release and QI
            global::WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<void*, IntPtr*, int>**)thisPtr)[3](thisPtr, &__retval));
            return __retval;
        }
    }

    public static class WindowNative
    {
        public static IntPtr GetWindowHandle(object target) => IWindowNativeMethods.get_WindowHandle(target);
    }

    internal static class IInitializeWithWindowMethods
    {
        private static ref readonly Guid IID_IInitializeWithWindow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0xBD, 0xD4, 0x68, 0x3E, 0x35, 0x71, 0x10, 0x4D, 0x80, 0x18, 0x9F, 0xB6, 0xD9, 0xF3, 0x3F, 0xA1
                ];

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static unsafe void Initialize(object _obj, IntPtr window)
        {
            using global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue objRefValue = WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeInterfaceMarshaller<object>.ConvertToUnmanaged(_obj, IID_IInitializeWithWindow);

            void* thisPtr = objRefValue.GetThisPtrUnsafe();

            // IInitializeWithWindow inherits IUnknown (3 functions) and provides Initialize giving a total of 4 functions
            global::WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<void*, IntPtr, int>**)thisPtr)[3](thisPtr, window));
        }
    }

    public static class InitializeWithWindow
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
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class DragDropManagerInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDragDropManagerInterop);
#endif

        public static CoreDragDropManager GetForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (CoreDragDropManager) global::ABI.WinRT.Interop.IDragDropManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_ApplicationModel_DataTransfer_DragDrop_Core_ICoreDragDropManager);
#endif
        }
    }
#endif
}

namespace Windows.Graphics.Printing
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PrintManagerInterop
    {
        private static ref readonly Guid IID_IAsyncOperation_bool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]   
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0xB3, 0xEF, 0xB5, 0xCD, 0x88, 0x57, 0x9D, 0x50, 0x9B, 0xE1, 0x71, 0xCC, 0xB8, 0xA3, 0x36, 0x2A
                ];

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Graphics.Printing.PrintManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPrintManagerInterop);
#endif

        public static PrintManager GetForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (PrintManager) global::ABI.WinRT.Interop.IPrintManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Printing_IPrintManager);
#endif
        }

        public static IAsyncOperation<bool> ShowPrintUIForWindowAsync(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (IAsyncOperation<bool>) global::ABI.WinRT.Interop.IPrintManagerInteropMethods.ShowPrintUIForWindowAsync(objectReference, appWindow, IID_IAsyncOperation_bool);
#endif
        }
    }
#endif
}

namespace Windows.Media
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class SystemMediaTransportControlsInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Media.SystemMediaTransportControls", global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISystemMediaTransportControlsInterop);
#endif

        public static SystemMediaTransportControls GetForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (SystemMediaTransportControls) global::ABI.WinRT.Interop.ISystemMediaTransportControlsInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Media_ISystemMediaTransportControls);
#endif
        }
    }
#endif
}

namespace Windows.Media.PlayTo
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PlayToManagerInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Media.PlayTo.PlayToManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPlayToManagerInterop);
#endif

        public static PlayToManager GetForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (PlayToManager) global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Media_PlayTo_IPlayToManager);
#endif
        }

        public static void ShowPlayToUIForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.ShowPlayToUIForWindow(objectReference, appWindow);
#endif
        }
    }
#endif
}

namespace Windows.Security.Credentials.UI
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UserConsentVerifierInterop
    {
        private static ref readonly Guid IID_IAsyncOperation_UserConsentVerificationResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0xFD, 0x6F, 0x59, 0xFD, 0x18, 0x23, 0x8F, 0x55, 0x9D, 0xBE, 0xD2, 0x1D, 0xF4, 0x37, 0x64, 0xA5
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Security.Credentials.UI.UserConsentVerifier", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUserConsentVerifierInterop);
#endif

        public static IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(IntPtr appWindow, string message)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (IAsyncOperation<UserConsentVerificationResult>) global::ABI.WinRT.Interop.IUserConsentVerifierInteropMethods.RequestVerificationForWindowAsync(objectReference, appWindow, message, IID_IAsyncOperation_UserConsentVerificationResult);
#endif
        }
    }
#endif
}

namespace Windows.Security.Authentication.Web.Core
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class WebAuthenticationCoreManagerInterop
    {
        private static ref readonly Guid IID_IAsyncOperation_WebTokenRequestResult
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x52, 0x58, 0x81, 0x0A, 0x44, 0x7C, 0x74, 0x56, 0xB3, 0xD2, 0xFA, 0x2E, 0x4C, 0x1E, 0x46, 0xC9
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IWebAuthenticationCoreManagerInterop);
#endif

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr appWindow, WebTokenRequest request)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (IAsyncOperation<WebTokenRequestResult>) global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenForWindowAsync(objectReference, appWindow, request, IID_IAsyncOperation_WebTokenRequestResult);
#endif
        }

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(IntPtr appWindow, WebTokenRequest request, WebAccount webAccount)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (IAsyncOperation<WebTokenRequestResult>) global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenWithWebAccountForWindowAsync(objectReference, appWindow, request, webAccount, IID_IAsyncOperation_WebTokenRequestResult);
#endif
        }
    }
#endif
}

namespace Windows.UI.ApplicationSettings
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class AccountsSettingsPaneInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.ApplicationSettings.AccountsSettingsPane", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IAccountsSettingsPaneInterop);
#endif

        public static AccountsSettingsPane GetForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (AccountsSettingsPane) global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_UI_ApplicationSettings_IAccountsSettingsPane);
#endif
        }

        public static IAsyncAction ShowManageAccountsForWindowAsync(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (IAsyncAction) global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowManageAccountsForWindowAsync(objectReference, appWindow, global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
        }

        public static IAsyncAction ShowAddAccountForWindowAsync(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (IAsyncAction) global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowAddAccountForWindowAsync(objectReference, appWindow, global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
        }
    }
#endif
}

namespace Windows.UI.Input
{
#if UAC_VERSION_3
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerConfigurationInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.RadialControllerConfiguration", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerConfigurationInterop);
#endif

        public static RadialControllerConfiguration GetForWindow(IntPtr hwnd)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (RadialControllerConfiguration) global::ABI.WinRT.Interop.IRadialControllerConfigurationInteropMethods.GetForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialControllerConfiguration);
#endif
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.RadialController", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerInterop);
#endif

        public static RadialController CreateForWindow(IntPtr hwnd)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (RadialController) global::ABI.WinRT.Interop.IRadialControllerInteropMethods.CreateForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialController);
#endif
        }
    }
#endif
}

namespace Windows.UI.Input.Core
{
#if UAC_VERSION_4
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.15063.0")]
    public static class RadialControllerIndependentInputSourceInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.Core.RadialControllerIndependentInputSource", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerIndependentInputSourceInterop);
#endif

        public static RadialControllerIndependentInputSource CreateForWindow(IntPtr hwnd)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (RadialControllerIndependentInputSource) global::ABI.WinRT.Interop.IRadialControllerIndependentInputSourceInteropMethods.CreateForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Core_IRadialControllerIndependentInputSource);
#endif
        }
    }
#endif
}

namespace Windows.UI.Input.Spatial
{
#if UAC_VERSION_2
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10586.0")]
    public static class SpatialInteractionManagerInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.Spatial.SpatialInteractionManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISpatialInteractionManagerInterop);
#endif

        public static SpatialInteractionManager GetForWindow(IntPtr window)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (SpatialInteractionManager) global::ABI.WinRT.Interop.ISpatialInteractionManagerInteropMethods.GetForWindow(objectReference, window, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Spatial_ISpatialInteractionManager);
#endif
        }
    }
#endif
}

namespace Windows.UI.ViewManagement
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class InputPaneInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.ViewManagement.InputPane", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IInputPaneInterop);
#endif

        public static InputPane GetForWindow(IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (InputPane) global::ABI.WinRT.Interop.IInputPaneInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IInputPane);
#endif
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UIViewSettingsInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.ViewManagement.UIViewSettings", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUIViewSettingsInterop);
#endif

        public static UIViewSettings GetForWindow(IntPtr hwnd)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (UIViewSettings) global::ABI.WinRT.Interop.IUIViewSettingsInteropMethods.GetForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IUIViewSettings);
#endif
        }
    }
#endif
}

namespace Windows.Graphics.Display
{
#if UAC_VERSION_15
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.22621.0")]
    public static class DisplayInformationInterop
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Graphics.Display.DisplayInformation", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDisplayInformationStaticsInterop);
#endif

        public static DisplayInformation GetForWindow(IntPtr window)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (DisplayInformation) global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForWindow(objectReference, window, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
        }

        public static DisplayInformation GetForMonitor(IntPtr monitor)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return (DisplayInformation) global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForMonitor(objectReference, monitor, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
        }
    }
#endif
}

namespace Windows.ApplicationModel.DataTransfer
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class DataTransferManagerInterop
    {
        private static ref readonly Guid Riid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x9B, 0xEE, 0xCA, 0xA5, 0x08, 0x87, 0xD1, 0x49, 0x8D, 0x36, 0x67, 0xD2, 0x5A, 0x8D, 0xA0, 0x0C
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.ApplicationModel.DataTransfer.DataTransferManager", IDataTransferManagerInteropMethods.IID);
#endif

        public static global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(global::System.IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return IDataTransferManagerInteropMethods.GetForWindow(objectReference, appWindow, Riid);
#endif
        }

        public static void ShowShareUIForWindow(global::System.IntPtr appWindow)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            IDataTransferManagerInteropMethods.ShowShareUIForWindow(objectReference, appWindow);
#endif
        }
    }
#endif

#if !CSWINRT_REFERENCE_PROJECTION
    internal static class IDataTransferManagerInteropMethods
    {
        internal static ref readonly Guid IID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x6C, 0xCD, 0x3D, 0x3A, 0xAB, 0x3E, 0xDC, 0x43, 0xBC, 0xDE, 0x45, 0x67, 0x1C, 0xE8, 0x00, 0xC8
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        internal static unsafe global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference _obj, global::System.IntPtr appWindow, in global::System.Guid riid)
        {
            using global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue activationFactoryValue = _obj.AsValue();
            void* thisPtr = activationFactoryValue.GetThisPtrUnsafe();
            void* ptr = null;

            try
            {
                // IDataTransferManagerInterop inherits IUnknown (3 functions) and provides GetForWindow giving a total of 5 functions
                fixed(Guid* _riid = &riid)
                global::WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<void*, global::System.IntPtr, global::System.Guid*, void**, int>**)thisPtr)[3](thisPtr, appWindow, _riid, &ptr));
                return global::ABI.Windows.ApplicationModel.DataTransfer.DataTransferManagerMarshaller.ConvertToManaged(ptr);
            }
            finally
            {
                global::WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeUnknownMarshaller.Free(ptr);
            }
        }

        internal static unsafe void ShowShareUIForWindow(global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference _obj, global::System.IntPtr appWindow)
        {
            using global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReferenceValue activationFactoryValue = _obj.AsValue();
            void* thisPtr = activationFactoryValue.GetThisPtrUnsafe();

            // IDataTransferManagerInterop inherits IUnknown (3 functions) and provides ShowShareUIForWindow giving a total of 5 functions
            global::WindowsRuntime.InteropServices.RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<void*, global::System.IntPtr, int>**)thisPtr)[4](thisPtr, appWindow));
        }
    }
#endif
}
