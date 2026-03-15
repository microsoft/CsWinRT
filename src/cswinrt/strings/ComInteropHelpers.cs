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

// All 'UAC_VERSION' checks for version '7' and below are not present,
// because the Windows SDK 17763 maps to version '7', and that's the
// minimum Windows SDK that is currently supported. See this mapping
// in the Windows SDK projection project. The two should be kept in sync.

#pragma warning disable CSWINRT3001

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using global::WindowsRuntime.InteropServices;
using global::WindowsRuntime.InteropServices.Marshalling;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
    /// <summary>
    /// Extensions for <see cref="CoreDragDropManager"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class CoreDragDropManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="CoreDragDropManager"/> activation factory, as <c>IDragDropManagerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.ApplicationModel.DataTransfer.DragDrop.Core.CoreDragDropManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDragDropManagerInterop);
#endif

        extension(CoreDragDropManager)
        {
            /// <summary>
            /// Gets a <see cref="CoreDragDropManager"/> object for the window of the active application.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="CoreDragDropManager"/> object</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the resulting <see cref="CoreDragDropManager"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/dragdropinterop/nf-dragdropinterop-idragdropmanagerinterop-getforwindow"/>
            public static CoreDragDropManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDragDropManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    hwnd: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_ApplicationModel_DataTransfer_DragDrop_Core_ICoreDragDropManager);
#endif
            }
        }
    }
}

namespace Windows.Graphics.Printing
{
    using Windows.Foundation;

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PrintManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Graphics.Printing.PrintManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPrintManagerInterop);

        /// <summary>The accessor for <c>__uuidof(IAsyncOperation&lt;bool&gt;)</c>.</summary>
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<bool>")]
        private static extern ref readonly Guid IID_IAsyncOperation_bool([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(PrintManager)
        {
            public static PrintManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IPrintManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Graphics_Printing_IPrintManager);
#endif
            }

            public static IAsyncOperation<bool> ShowPrintUIForWindowAsync(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IPrintManagerInteropMethods.ShowPrintUIForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in IID_IAsyncOperation_bool(null));
#endif
            }
        }
    }
}

namespace Windows.Media
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class SystemMediaTransportControlsExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Media.SystemMediaTransportControls",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISystemMediaTransportControlsInterop);
#endif

        extension(SystemMediaTransportControls)
        {
            public static SystemMediaTransportControls GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.ISystemMediaTransportControlsInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Media_ISystemMediaTransportControls);
#endif
            }
        }
    }
}

namespace Windows.Media.PlayTo
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PlayToManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Media.PlayTo.PlayToManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPlayToManagerInterop);
#endif

        extension(PlayToManager)
        {
            public static PlayToManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Media_PlayTo_IPlayToManager);
#endif
            }

            public static void ShowPlayToUIForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                global::ABI.WinRT.Interop.IPlayToManagerInteropMethods.ShowPlayToUIForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow);
#endif
            }
        }
    }
}

namespace Windows.Security.Credentials.UI
{
    using Windows.Foundation;
    using Windows.Security.Credentials;

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UserConsentVerifierExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Security.Credentials.UI.UserConsentVerifier",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUserConsentVerifierInterop);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<<#Windows>Windows-Security-Credentials-UI-UserConsentVerificationResult>")]
        private static extern ref readonly Guid IID_IAsyncOperation_UserConsentVerificationResult([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(UserConsentVerifier)
        {
            public static IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(nint appWindow, string message)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IUserConsentVerifierInteropMethods.RequestVerificationForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    message: message,
                    riid: in IID_IAsyncOperation_UserConsentVerificationResult(null));
#endif
            }
        }
    }
}

namespace Windows.Security.Authentication.Web.Core
{
    using Windows.Foundation;

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class WebAuthenticationCoreManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IWebAuthenticationCoreManagerInterop);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<<#Windows>Windows-Security-Authentication-Web-Core-WebTokenRequestResult>")]
        private static extern ref readonly Guid IID_IAsyncOperation_WebTokenRequestResult([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(WebAuthenticationCoreManager)
        {
            public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(nint appWindow, WebTokenRequest request)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    request: request,
                    riid: in IID_IAsyncOperation_WebTokenRequestResult(null));
#endif
            }

            public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(nint appWindow, WebTokenRequest request, WebAccount webAccount)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IWebAuthenticationCoreManagerInteropMethods.RequestTokenWithWebAccountForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    request: request,
                    webAccount: webAccount,
                    riid: in IID_IAsyncOperation_WebTokenRequestResult(null));
#endif
            }
        }
    }
}

namespace Windows.UI.ApplicationSettings
{
    using Windows.Foundation;

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class AccountsSettingsPaneExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.ApplicationSettings.AccountsSettingsPane",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IAccountsSettingsPaneInterop);
#endif

        extension(AccountsSettingsPane)
        {
            public static AccountsSettingsPane GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_ApplicationSettings_IAccountsSettingsPane);
#endif
            }

            public static IAsyncAction ShowManageAccountsForWindowAsync(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowManageAccountsForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
            }

            public static IAsyncAction ShowAddAccountForWindowAsync(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IAccountsSettingsPaneInteropMethods.ShowAddAccountForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
            }
        }
    }
}

namespace Windows.UI.Input
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerConfigurationExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.RadialControllerConfiguration",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerConfigurationInterop);
#endif

        extension(RadialControllerConfiguration)
        {
            public static RadialControllerConfiguration GetForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IRadialControllerConfigurationInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    hwnd: hwnd,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialControllerConfiguration);
#endif
            }
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.RadialController",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerInterop);
#endif

        extension(RadialController)
        {
            public static RadialController CreateForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IRadialControllerInteropMethods.CreateForWindow(
                    thisReference: objectReference,
                    hwnd: hwnd,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialController);
#endif
            }
        }
    }
}

namespace Windows.UI.Input.Core
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.15063.0")]
    public static class RadialControllerIndependentInputSourceExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.Core.RadialControllerIndependentInputSource",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerIndependentInputSourceInterop);
#endif

        extension(RadialControllerIndependentInputSource)
        {
            public static RadialControllerIndependentInputSource CreateForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IRadialControllerIndependentInputSourceInteropMethods.CreateForWindow(
                    thisReference: objectReference,
                    hwnd: hwnd,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Core_IRadialControllerIndependentInputSource);
#endif
            }
        }
    }
}

namespace Windows.UI.Input.Spatial
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10586.0")]
    public static class SpatialInteractionManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.Spatial.SpatialInteractionManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISpatialInteractionManagerInterop);
#endif

        extension(SpatialInteractionManager)
        {
            public static SpatialInteractionManager GetForWindow(nint window)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.ISpatialInteractionManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    window: window,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_Input_Spatial_ISpatialInteractionManager);
#endif
            }
        }
    }
}

namespace Windows.UI.ViewManagement
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class InputPaneExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.ViewManagement.InputPane",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IInputPaneInterop);
#endif

        extension(InputPane)
        {
            public static InputPane GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IInputPaneInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IInputPane);
#endif
            }
        }
    }

    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UIViewSettingsExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.ViewManagement.UIViewSettings",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUIViewSettingsInterop);
#endif

        extension(UIViewSettings)
        {
            public static UIViewSettings GetForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IUIViewSettingsInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    hwnd: hwnd,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IUIViewSettings);
#endif
            }
        }
    }
}

namespace Windows.Graphics.Display
{
#if UAC_VERSION_15
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.22621.0")]
    public static class DisplayInformationExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Graphics.Display.DisplayInformation",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDisplayInformationStaticsInterop);
#endif

        extension(DisplayInformation)
        {
            public static DisplayInformation GetForWindow(nint window)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    window: window,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
            }

            public static DisplayInformation GetForMonitor(nint monitor)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDisplayInformationStaticsInteropMethods.GetForMonitor(
                    thisReference: objectReference,
                    monitor: monitor,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
            }
        }
    }
#endif
}

namespace Windows.ApplicationModel.DataTransfer
{
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class DataTransferManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.ApplicationModel.DataTransfer.DataTransferManager",
            iid: in IDataTransferManagerInteropMethods.IID);
#endif

        extension(DataTransferManager)
        {
            public static global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WinRT.Interop.IDataTransferManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IDataTransferManager);
#endif
            }

            public static void ShowShareUIForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                global::ABI.WinRT.Interop.IDataTransferManagerInteropMethods.ShowShareUIForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow);
#endif
            }
        }
    }
}

namespace ABI.WinRT.Interop
{
#if !CSWINRT_REFERENCE_PROJECTION
    internal static class IDataTransferManagerInteropMethods
    {
        public static ref readonly Guid IID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x6C, 0xCD, 0x3D, 0x3A,
                    0xAB, 0x3E,
                    0xDC, 0x43,
                    0xBC,
                    0xDE,
                    0x45,
                    0x67,
                    0x1C,
                    0xE8,
                    0x00,
                    0xC8
                ];

                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static unsafe global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(
            WindowsRuntimeObjectReference thisReference,
            nint appWindow,
            in Guid riid)
        {
            using WindowsRuntimeObjectReferenceValue activationFactoryValue = thisReference.AsValue();

            void* thisPtr = activationFactoryValue.GetThisPtrUnsafe();
            void* result = null;

            try
            {
                fixed (Guid* _riid = &riid)
                {
                    int hresult = (*(delegate* unmanaged[MemberFunction]<void*, nint, Guid*, void**, int>**)thisPtr)[3](thisPtr, appWindow, _riid, &result);

                    RestrictedErrorInfo.ThrowExceptionForHR(hresult);
                }

                return global::ABI.Windows.ApplicationModel.DataTransfer.DataTransferManagerMarshaller.ConvertToManaged(result);
            }
            finally
            {
                WindowsRuntimeUnknownMarshaller.Free(result);
            }
        }

        public static unsafe void ShowShareUIForWindow(WindowsRuntimeObjectReference thisReference, nint appWindow)
        {
            using WindowsRuntimeObjectReferenceValue activationFactoryValue = thisReference.AsValue();

            void* thisPtr = activationFactoryValue.GetThisPtrUnsafe();

            int hresult = (*(delegate* unmanaged[MemberFunction]<void*, nint, int>**)thisPtr)[4](thisPtr, appWindow);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }
#endif
}
