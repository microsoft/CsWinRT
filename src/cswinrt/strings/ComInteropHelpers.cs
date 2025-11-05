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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDragDropManagerInterop);
        
        public static CoreDragDropManager GetForWindow(IntPtr appWindow)
        {
            return (CoreDragDropManager) global::ABI.WinRT.Interop.IDragDropManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_ApplicationModel_DataTransfer_DragDrop_Core_ICoreDragDropManager);
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

        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.Graphics.Printing.PrintManager.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPrintManagerInterop);

        public static PrintManager GetForWindow(IntPtr appWindow)
        {
            return (PrintManager) global::ABI.WinRT.Interop.IPrintManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Printing_IPrintManager);
        }

        public static IAsyncOperation<bool> ShowPrintUIForWindowAsync(IntPtr appWindow)
        {
            return (IAsyncOperation<bool>) global::ABI.WinRT.Interop.IPrintManagerInteropMethods.ShowPrintUIForWindowAsync(objectReference, appWindow, IID_IAsyncOperation_bool);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.Media.SystemMediaTransportControls.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISystemMediaTransportControlsInterop);

        public static SystemMediaTransportControls GetForWindow(IntPtr appWindow)
        {
            return (SystemMediaTransportControls) global::ABI.WinRT.Interop.ISystemMediaTransportControlsInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Media_ISystemMediaTransportControls);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.Media.PlayTo.PlayToManager.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPlayToManagerInterop);
        
        public static PlayToManager GetForWindow(IntPtr appWindow)
        {
            return (PlayToManager) global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Media_PlayTo_IPlayToManager);
        }

        public static void ShowPlayToUIForWindow(IntPtr appWindow)
        {
            global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.ShowPlayToUIForWindow(objectReference, appWindow);
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

        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.Security.Credentials.UI.UserConsentVerifier.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUserConsentVerifierInterop);

        public static IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(IntPtr appWindow, string message)
        {
            return (IAsyncOperation<UserConsentVerificationResult>) global::ABI.WinRT.Interop.IUserConsentVerifierInteropMethods.RequestVerificationForWindowAsync(objectReference, appWindow, message, IID_IAsyncOperation_UserConsentVerificationResult);
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

        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IWebAuthenticationCoreManagerInterop);

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr appWindow, WebTokenRequest request)
        {
            return (IAsyncOperation<WebTokenRequestResult>) global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenForWindowAsync(objectReference, appWindow, request, IID_IAsyncOperation_WebTokenRequestResult);
        }

        public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(IntPtr appWindow, WebTokenRequest request, WebAccount webAccount)
        {
            return (IAsyncOperation<WebTokenRequestResult>) global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenWithWebAccountForWindowAsync(objectReference, appWindow, request, webAccount, IID_IAsyncOperation_WebTokenRequestResult);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.ApplicationSettings.AccountsSettingsPane.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IAccountsSettingsPaneInterop);

        public static AccountsSettingsPane GetForWindow(IntPtr appWindow)
        {
            return (AccountsSettingsPane) global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_UI_ApplicationSettings_IAccountsSettingsPane);
        }

        public static IAsyncAction ShowManageAccountsForWindowAsync(IntPtr appWindow)
        {
            return (IAsyncAction) global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowManageAccountsForWindowAsync(objectReference, appWindow, global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
        }

        public static IAsyncAction ShowAddAccountForWindowAsync(IntPtr appWindow)
        {
            return (IAsyncAction) global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowAddAccountForWindowAsync(objectReference, appWindow, global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.Input.RadialControllerConfiguration.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerConfigurationInterop);
        
        public static RadialControllerConfiguration GetForWindow(IntPtr hwnd)
        {
            return (RadialControllerConfiguration) global::ABI.WinRT.Interop.IRadialControllerConfigurationInteropMethods.GetForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialControllerConfiguration);
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerInterop
    {
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.Input.RadialController.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerInterop);

        public static RadialController CreateForWindow(IntPtr hwnd)
        {
            return (RadialController) global::ABI.WinRT.Interop.IRadialControllerInteropMethods.CreateForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialController);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.Input.Core.RadialControllerIndependentInputSource.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerIndependentInputSourceInterop);

        public static RadialControllerIndependentInputSource CreateForWindow(IntPtr hwnd)
        {
            return (RadialControllerIndependentInputSource) global::ABI.WinRT.Interop.IRadialControllerIndependentInputSourceInteropMethods.CreateForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Core_IRadialControllerIndependentInputSource);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.Input.Spatial.SpatialInteractionManager.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISpatialInteractionManagerInterop);
        
        public static SpatialInteractionManager GetForWindow(IntPtr window)
        {
            return (SpatialInteractionManager) global::ABI.WinRT.Interop.ISpatialInteractionManagerInteropMethods.GetForWindow(objectReference, window, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Spatial_ISpatialInteractionManager);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.ViewManagement.InputPane.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IInputPaneInterop);

        public static InputPane GetForWindow(IntPtr appWindow)
        {
            return (InputPane) global::ABI.WinRT.Interop.IInputPaneInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IInputPane);
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UIViewSettingsInterop
    {
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.UI.ViewManagement.UIViewSettings.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUIViewSettingsInterop);

        public static UIViewSettings GetForWindow(IntPtr hwnd)
        {
            return (UIViewSettings) global::ABI.WinRT.Interop.IUIViewSettingsInteropMethods.GetForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IUIViewSettings);
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
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.Graphics.Display.DisplayInformation.RuntimeClassName, global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDisplayInformationStaticsInterop);

        public static DisplayInformation GetForWindow(IntPtr window)
        {
            return (DisplayInformation) global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForWindow(objectReference, window, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
        }

        public static DisplayInformation GetForMonitor(IntPtr monitor)
        {
            return (DisplayInformation) global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForMonitor(objectReference, monitor, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
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

        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory(ABI.Windows.ApplicationModel.DataTransfer.DataTransferManager.RuntimeClassName, IDataTransferManagerInteropMethods.IID);

        public static global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(global::System.IntPtr appWindow)
        {
            return IDataTransferManagerInteropMethods.GetForWindow(objectReference, appWindow, Riid);
        }

        public static void ShowShareUIForWindow(global::System.IntPtr appWindow)
        {
            IDataTransferManagerInteropMethods.ShowShareUIForWindow(objectReference, appWindow);
        }
    }
#endif

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
}
