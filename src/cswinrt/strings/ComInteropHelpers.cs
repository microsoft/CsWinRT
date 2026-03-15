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

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class CoreDragDropManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDragDropManagerInterop);
#endif

        extension(CoreDragDropManager)
        {
            public static CoreDragDropManager GetForWindow(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDragDropManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_ApplicationModel_DataTransfer_DragDrop_Core_ICoreDragDropManager);
#endif
            }
        }
    }
#endif
}

namespace Windows.Graphics.Printing
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PrintManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Graphics.Printing.PrintManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPrintManagerInterop);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<bool>")]
        static extern ref readonly Guid IID_IAsyncOperation_bool([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(PrintManager)
        {
            public static PrintManager GetForWindow(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IPrintManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Printing_IPrintManager);
#endif
            }

            public static IAsyncOperation<bool> ShowPrintUIForWindowAsync(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IPrintManagerInteropMethods.ShowPrintUIForWindowAsync(objectReference, appWindow, IID_IAsyncOperation_bool(null));
#endif
            }
        }
    }
#endif
}

namespace Windows.Media
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class SystemMediaTransportControlsExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Media.SystemMediaTransportControls", global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISystemMediaTransportControlsInterop);
#endif

        extension(SystemMediaTransportControls)
        {
            public static SystemMediaTransportControls GetForWindow(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.ISystemMediaTransportControlsInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Media_ISystemMediaTransportControls);
#endif
            }
        }
    }
#endif
}

namespace Windows.Media.PlayTo
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PlayToManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Media.PlayTo.PlayToManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPlayToManagerInterop);
#endif

        extension(PlayToManager)
        {
            public static PlayToManager GetForWindow(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_Media_PlayTo_IPlayToManager);
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
    }
#endif
}

namespace Windows.Security.Credentials.UI
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UserConsentVerifierExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Security.Credentials.UI.UserConsentVerifier", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUserConsentVerifierInterop);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<<#Windows>Windows-Security-Credentials-UI-UserConsentVerificationResult>")]
        static extern ref readonly Guid IID_IAsyncOperation_UserConsentVerificationResult([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(UserConsentVerifier)
        {
            public static IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(IntPtr appWindow, string message)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IUserConsentVerifierInteropMethods.RequestVerificationForWindowAsync(objectReference, appWindow, message, IID_IAsyncOperation_UserConsentVerificationResult(null));
#endif
            }
        }
    }
#endif
}

namespace Windows.Security.Authentication.Web.Core
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class WebAuthenticationCoreManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IWebAuthenticationCoreManagerInterop);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<<#Windows>Windows-Security-Authentication-Web-Core-WebTokenRequestResult>")]
        static extern ref readonly Guid IID_IAsyncOperation_WebTokenRequestResult([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(WebAuthenticationCoreManager)
        {
            public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(IntPtr appWindow, WebTokenRequest request)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return (IAsyncOperation<WebTokenRequestResult>) global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenForWindowAsync(objectReference, appWindow, request, IID_IAsyncOperation_WebTokenRequestResult(null));
#endif
            }

            public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(IntPtr appWindow, WebTokenRequest request, WebAccount webAccount)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return (IAsyncOperation<WebTokenRequestResult>) global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenWithWebAccountForWindowAsync(objectReference, appWindow, request, webAccount, IID_IAsyncOperation_WebTokenRequestResult(null));
#endif
            }
        }
    }
#endif
}

namespace Windows.UI.ApplicationSettings
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class AccountsSettingsPaneExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.ApplicationSettings.AccountsSettingsPane", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IAccountsSettingsPaneInterop);
#endif

        extension(AccountsSettingsPane)
        {
            public static AccountsSettingsPane GetForWindow(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_UI_ApplicationSettings_IAccountsSettingsPane);
#endif
            }

            public static IAsyncAction ShowManageAccountsForWindowAsync(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowManageAccountsForWindowAsync(objectReference, appWindow, global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
            }

            public static IAsyncAction ShowAddAccountForWindowAsync(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowAddAccountForWindowAsync(objectReference, appWindow, global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
            }
        }
    }
#endif
}

namespace Windows.UI.Input
{
#if UAC_VERSION_3
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerConfigurationExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.RadialControllerConfiguration", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerConfigurationInterop);
#endif

        extension(RadialControllerConfiguration)
        {
            public static RadialControllerConfiguration GetForWindow(IntPtr hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IRadialControllerConfigurationInteropMethods.GetForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialControllerConfiguration);
#endif
            }
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.RadialController", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerInterop);
#endif

        extension(RadialController)
        {
            public static RadialController CreateForWindow(IntPtr hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IRadialControllerInteropMethods.CreateForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialController);
#endif
            }
        }
    }
#endif
}

namespace Windows.UI.Input.Core
{
#if UAC_VERSION_4
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.15063.0")]
    public static class RadialControllerIndependentInputSourceExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.Core.RadialControllerIndependentInputSource", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerIndependentInputSourceInterop);
#endif

        extension(RadialControllerIndependentInputSource)
        {
            public static RadialControllerIndependentInputSource CreateForWindow(IntPtr hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IRadialControllerIndependentInputSourceInteropMethods.CreateForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Core_IRadialControllerIndependentInputSource);
#endif
            }
        }
    }
#endif
}

namespace Windows.UI.Input.Spatial
{
#if UAC_VERSION_2
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10586.0")]
    public static class SpatialInteractionManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.Input.Spatial.SpatialInteractionManager", global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISpatialInteractionManagerInterop);
#endif

        extension(SpatialInteractionManager)
        {
            public static SpatialInteractionManager GetForWindow(IntPtr window)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.ISpatialInteractionManagerInteropMethods.GetForWindow(objectReference, window, global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Spatial_ISpatialInteractionManager);
#endif
            }
        }
    }
#endif
}

namespace Windows.UI.ViewManagement
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class InputPaneExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.ViewManagement.InputPane", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IInputPaneInterop);
#endif

        extension(InputPane)
        {
            public static InputPane GetForWindow(IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IInputPaneInteropMethods.GetForWindow(objectReference, appWindow, global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IInputPane);
#endif
            }
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UIViewSettingsExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.UI.ViewManagement.UIViewSettings", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUIViewSettingsInterop);
#endif

        extension(UIViewSettings)
        {
            public static UIViewSettings GetForWindow(IntPtr hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IUIViewSettingsInteropMethods.GetForWindow(objectReference, hwnd, global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IUIViewSettings);
#endif
            }
        }
    }
#endif
}

namespace Windows.Graphics.Display
{
#if UAC_VERSION_15
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.22621.0")]
    public static class DisplayInformationExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.Graphics.Display.DisplayInformation", global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDisplayInformationStaticsInterop);
#endif

        extension(DisplayInformation)
        {
            public static DisplayInformation GetForWindow(IntPtr window)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForWindow(objectReference, window, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
            }

            public static DisplayInformation GetForMonitor(IntPtr monitor)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForMonitor(objectReference, monitor, global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
            }
        }
    }
#endif
}

namespace Windows.ApplicationModel.DataTransfer
{
#if UAC_VERSION_1
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class DataTransferManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly global::WindowsRuntime.InteropServices.WindowsRuntimeObjectReference objectReference = global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory.GetActivationFactory("Windows.ApplicationModel.DataTransfer.DataTransferManager", IDataTransferManagerInteropMethods.IID);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#Windows>IDataTransferManager")]
        static extern ref readonly Guid IID_IDataTransferManager([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(DataTransferManager)
        {
            public static global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(global::System.IntPtr appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return IDataTransferManagerInteropMethods.GetForWindow(objectReference, appWindow, IID_IDataTransferManager(null));
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
