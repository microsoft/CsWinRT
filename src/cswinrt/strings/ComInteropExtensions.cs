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
                return global::ABI.WindowsRuntime.Internal.IDragDropManagerInteropMethods.GetForWindow(
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

    /// <summary>
    /// Extensions for <see cref="PrintManager"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PrintManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="PrintManager"/> activation factory, as <c>IPrintManagerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Graphics.Printing.PrintManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPrintManagerInterop);

        /// <summary>The accessor for <c>__uuidof(IAsyncOperation&lt;bool&gt;)</c>.</summary>
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<bool>")]
        private static extern ref readonly Guid IID_IAsyncOperation_bool([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(PrintManager)
        {
            /// <summary>
            /// Gets the <see cref="PrintManager"/> instance for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window to get the <see cref="PrintManager"/> instance for (an <c>HWND</c>).</param>
            /// <returns>The <see cref="PrintManager"/> instance for the specified window.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="PrintManager"/> instance.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/printmanagerinterop/nf-printmanagerinterop-iprintmanagerinterop-getforwindow"/>
            public static PrintManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IPrintManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Graphics_Printing_IPrintManager);
#endif
            }

            /// <summary>
            /// Displays the UI for printing content for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window to show the print UI for (an <c>HWND</c>).</param>
            /// <returns>An asynchronous operation that reports completion.</returns>
            /// <exception cref="Exception">Thrown if failed to display the print UI.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/printmanagerinterop/nf-printmanagerinterop-iprintmanagerinterop-showprintuiforwindowasync"/>
            public static IAsyncOperation<bool> ShowPrintUIForWindowAsync(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IPrintManagerInteropMethods.ShowPrintUIForWindowAsync(
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
    /// <summary>
    /// Extensions for <see cref="SystemMediaTransportControls"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class SystemMediaTransportControlsExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="SystemMediaTransportControls"/> activation factory, as <c>ISystemMediaTransportControlsInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Media.SystemMediaTransportControls",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISystemMediaTransportControlsInterop);
#endif

        extension(SystemMediaTransportControls)
        {
            /// <summary>
            /// Gets an instance of <see cref="SystemMediaTransportControls"/> for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the top-level window for which the <see cref="SystemMediaTransportControls"/> instance is retrieved (an <c>HWND</c>).</param>
            /// <returns>The <see cref="SystemMediaTransportControls"/> instance for the specified window.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="SystemMediaTransportControls"/> instance.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/systemmediatransportcontrolsinterop/nf-systemmediatransportcontrolsinterop-isystemmediatransportcontrolsinterop-getforwindow"/>
            public static SystemMediaTransportControls GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.ISystemMediaTransportControlsInteropMethods.GetForWindow(
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
    /// <summary>
    /// Extensions for <see cref="PlayToManager"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class PlayToManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="PlayToManager"/> activation factory, as <c>IPlayToManagerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Media.PlayTo.PlayToManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IPlayToManagerInterop);
#endif

        extension(PlayToManager)
        {
            /// <summary>
            /// Gets the <see cref="PlayToManager"/> instance for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window to get the <see cref="PlayToManager"/> instance for (an <c>HWND</c>).</param>
            /// <returns>The <see cref="PlayToManager"/> instance for the specified window.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="PlayToManager"/> instance.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/playtomanagerinterop/nf-playtomanagerinterop-iplaytomanagerinterop-getforwindow"/>
            public static PlayToManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IPlayToManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Media_PlayTo_IPlayToManager);
#endif
            }

            /// <summary>
            /// Displays the Play To UI for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window to show the Play To UI for (an <c>HWND</c>).</param>
            /// <exception cref="Exception">Thrown if failed to display the Play To UI.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/playtomanagerinterop/nf-playtomanagerinterop-iplaytomanagerinterop-showplaytouiforwindow"/>
            public static void ShowPlayToUIForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                global::ABI.WindowsRuntime.Internal.IPlayToManagerInteropMethods.ShowPlayToUIForWindow(
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

    /// <summary>
    /// Extensions for <see cref="UserConsentVerifier"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UserConsentVerifierExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="UserConsentVerifier"/> activation factory, as <c>IUserConsentVerifierInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Security.Credentials.UI.UserConsentVerifier",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUserConsentVerifierInterop);

        /// <summary>The accessor for <c>__uuidof(IAsyncOperation&lt;UserConsentVerificationResult&gt;)</c>.</summary>
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<<#Windows>Windows-Security-Credentials-UI-UserConsentVerificationResult>")]
        private static extern ref readonly Guid IID_IAsyncOperation_UserConsentVerificationResult([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(UserConsentVerifier)
        {
            /// <summary>
            /// Performs a verification using a device such as Microsoft Passport PIN, Windows Hello, or a fingerprint reader.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <param name="message">A message to display to the user for this biometric verification request.</param>
            /// <returns>An asynchronous operation with a <see cref="UserConsentVerificationResult"/> value describing the result of the verification.</returns>
            /// <exception cref="Exception">Thrown if the verification request failed.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/userconsentverifierinterop/nf-userconsentverifierinterop-iuserconsentverifierinterop-requestverificationforwindowasync"/>
            public static IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(nint appWindow, string message)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IUserConsentVerifierInteropMethods.RequestVerificationForWindowAsync(
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

    /// <summary>
    /// Extensions for <see cref="WebAuthenticationCoreManager"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class WebAuthenticationCoreManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="WebAuthenticationCoreManager"/> activation factory, as <c>IWebAuthenticationCoreManagerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Security.Authentication.Web.Core.WebAuthenticationCoreManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IWebAuthenticationCoreManagerInterop);

        /// <summary>The accessor for <c>__uuidof(IAsyncOperation&lt;WebTokenRequestResult&gt;)</c>.</summary>
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_<#CsWinRT>IAsyncOperation'1<<#Windows>Windows-Security-Authentication-Web-Core-WebTokenRequestResult>")]
        private static extern ref readonly Guid IID_IAsyncOperation_WebTokenRequestResult([UnsafeAccessorType("<InterfaceIIDs>, WinRT.Interop")] object _);
#endif

        extension(WebAuthenticationCoreManager)
        {
            /// <summary>
            /// Asynchronously requests a token from a web account provider. If necessary, the user is prompted to enter their credentials.
            /// </summary>
            /// <param name="appWindow">The handle to the window to be used as the owner for the window prompting the user for credentials, in case such a window becomes necessary (an <c>HWND</c>).</param>
            /// <param name="request">The web token request.</param>
            /// <returns>An asynchronous operation with the <see cref="WebTokenRequestResult"/> for the request.</returns>
            /// <exception cref="Exception">Thrown if the token request failed.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/webauthenticationcoremanagerinterop/nf-webauthenticationcoremanagerinterop-iwebauthenticationcoremanagerinterop-requesttokenforwindowasync"/>
            public static IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(nint appWindow, WebTokenRequest request)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IWebAuthenticationCoreManagerInteropMethods.RequestTokenForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    request: request,
                    riid: in IID_IAsyncOperation_WebTokenRequestResult(null));
#endif
            }

            /// <summary>
            /// Asynchronously requests a token from a web account provider. If necessary, the user is prompted to enter their credentials.
            /// </summary>
            /// <param name="appWindow">The handle to the window to be used as the owner for the window prompting the user for credentials, in case such a window becomes necessary (an <c>HWND</c>).</param>
            /// <param name="request">The web token request.</param>
            /// <param name="webAccount">The web account for the request.</param>
            /// <returns>An asynchronous operation with the <see cref="WebTokenRequestResult"/> for the request.</returns>
            /// <exception cref="Exception">Thrown if the token request failed.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/webauthenticationcoremanagerinterop/nf-webauthenticationcoremanagerinterop-iwebauthenticationcoremanagerinterop-requesttokenwithwebaccountforwindowasync"/>
            public static IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(nint appWindow, WebTokenRequest request, WebAccount webAccount)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IWebAuthenticationCoreManagerInteropMethods.RequestTokenWithWebAccountForWindowAsync(
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

    /// <summary>
    /// Extensions for <see cref="AccountsSettingsPane"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class AccountsSettingsPaneExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="AccountsSettingsPane"/> activation factory, as <c>IAccountsSettingsPaneInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.ApplicationSettings.AccountsSettingsPane",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IAccountsSettingsPaneInterop);
#endif

        extension(AccountsSettingsPane)
        {
            /// <summary>
            /// Gets an <see cref="AccountsSettingsPane"/> object for the window of the active application.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="AccountsSettingsPane"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="AccountsSettingsPane"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/accountssettingspaneinterop/nf-accountssettingspaneinterop-iaccountssettingspaneinterop-getforwindow"/>
            public static AccountsSettingsPane GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IAccountsSettingsPaneInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_ApplicationSettings_IAccountsSettingsPane);
#endif
            }

            /// <summary>
            /// Displays the manage accounts screen for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>An asynchronous action that reports completion.</returns>
            /// <exception cref="Exception">Thrown if failed to display the manage accounts screen.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/accountssettingspaneinterop/nf-accountssettingspaneinterop-iaccountssettingspaneinterop-showmanageaccountsforwindowasync"/>
            public static IAsyncAction ShowManageAccountsForWindowAsync(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IAccountsSettingsPaneInteropMethods.ShowManageAccountsForWindowAsync(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_Windows_Foundation_IAsyncAction);
#endif
            }

            /// <summary>
            /// Displays the add accounts screen for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>An asynchronous action that reports completion.</returns>
            /// <exception cref="Exception">Thrown if failed to display the add accounts screen.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/accountssettingspaneinterop/nf-accountssettingspaneinterop-iaccountssettingspaneinterop-showaddaccountforwindowasync"/>
            public static IAsyncAction ShowAddAccountForWindowAsync(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IAccountsSettingsPaneInteropMethods.ShowAddAccountForWindowAsync(
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
    /// <summary>
    /// Extensions for <see cref="RadialControllerConfiguration"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerConfigurationExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="RadialControllerConfiguration"/> activation factory, as <c>IRadialControllerConfigurationInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.RadialControllerConfiguration",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerConfigurationInterop);
#endif

        extension(RadialControllerConfiguration)
        {
            /// <summary>
            /// Retrieves a <see cref="RadialControllerConfiguration"/> object bound to the active application.
            /// </summary>
            /// <param name="hwnd">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="RadialControllerConfiguration"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="RadialControllerConfiguration"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/radialcontrollerinterop/nf-radialcontrollerinterop-iradialcontrollerconfigurationinterop-getforwindow"/>
            public static RadialControllerConfiguration GetForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IRadialControllerConfigurationInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    hwnd: hwnd,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_Input_IRadialControllerConfiguration);
#endif
            }
        }
    }

    /// <summary>
    /// Extensions for <see cref="RadialController"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.14393.0")]
    public static class RadialControllerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="RadialController"/> activation factory, as <c>IRadialControllerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.RadialController",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerInterop);
#endif

        extension(RadialController)
        {
            /// <summary>
            /// Instantiates a <see cref="RadialController"/> object and binds it to the active application.
            /// </summary>
            /// <param name="hwnd">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="RadialController"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to create the <see cref="RadialController"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/radialcontrollerinterop/nf-radialcontrollerinterop-iradialcontrollerinterop-createforwindow"/>
            public static RadialController CreateForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IRadialControllerInteropMethods.CreateForWindow(
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
    /// <summary>
    /// Extensions for <see cref="RadialControllerIndependentInputSource"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.15063.0")]
    public static class RadialControllerIndependentInputSourceExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="RadialControllerIndependentInputSource"/> activation factory, as <c>IRadialControllerIndependentInputSourceInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.Core.RadialControllerIndependentInputSource",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IRadialControllerIndependentInputSourceInterop);
#endif

        extension(RadialControllerIndependentInputSource)
        {
            /// <summary>
            /// Instantiates a <see cref="RadialControllerIndependentInputSource"/> object and binds it to the active application.
            /// </summary>
            /// <param name="hwnd">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="RadialControllerIndependentInputSource"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to create the <see cref="RadialControllerIndependentInputSource"/> object.</exception>
            public static RadialControllerIndependentInputSource CreateForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IRadialControllerIndependentInputSourceInteropMethods.CreateForWindow(
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
    /// <summary>
    /// Extensions for <see cref="SpatialInteractionManager"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10586.0")]
    public static class SpatialInteractionManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="SpatialInteractionManager"/> activation factory, as <c>ISpatialInteractionManagerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.Input.Spatial.SpatialInteractionManager",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_ISpatialInteractionManagerInterop);
#endif

        extension(SpatialInteractionManager)
        {
            /// <summary>
            /// Retrieves a <see cref="SpatialInteractionManager"/> object bound to the active application.
            /// </summary>
            /// <param name="window">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="SpatialInteractionManager"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="SpatialInteractionManager"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/spatialinteractionmanagerinterop/nf-spatialinteractionmanagerinterop-ispatialinteractionmanagerinterop-getforwindow"/>
            public static SpatialInteractionManager GetForWindow(nint window)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.ISpatialInteractionManagerInteropMethods.GetForWindow(
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
    /// <summary>
    /// Extensions for <see cref="InputPane"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class InputPaneExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="InputPane"/> activation factory, as <c>IInputPaneInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.ViewManagement.InputPane",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IInputPaneInterop);
#endif

        extension(InputPane)
        {
            /// <summary>
            /// Gets an instance of an <see cref="InputPane"/> object for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the top-level window for which to get the <see cref="InputPane"/> object (an <c>HWND</c>).</param>
            /// <returns>The <see cref="InputPane"/> object for the specified window.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="InputPane"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/inputpaneinterop/nf-inputpaneinterop-iinputpaneinterop-getforwindow"/>
            public static InputPane GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IInputPaneInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_UI_ViewManagement_IInputPane);
#endif
            }
        }
    }

    /// <summary>
    /// Extensions for <see cref="UIViewSettings"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class UIViewSettingsExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="UIViewSettings"/> activation factory, as <c>IUIViewSettingsInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.UI.ViewManagement.UIViewSettings",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IUIViewSettingsInterop);
#endif

        extension(UIViewSettings)
        {
            /// <summary>
            /// Gets a <see cref="UIViewSettings"/> object for the window of the active application.
            /// </summary>
            /// <param name="hwnd">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="UIViewSettings"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="UIViewSettings"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/uiviewsettingsinterop/nf-uiviewsettingsinterop-iuiviewsettingsinterop-getforwindow"/>
            public static UIViewSettings GetForWindow(nint hwnd)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IUIViewSettingsInteropMethods.GetForWindow(
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
    /// <summary>
    /// Extensions for <see cref="DisplayInformation"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.22621.0")]
    public static class DisplayInformationExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="DisplayInformation"/> activation factory, as <c>IDisplayInformationStaticsInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.Graphics.Display.DisplayInformation",
            iid: in global::ABI.InterfaceIIDs.IID_WinRT_Interop_IDisplayInformationStaticsInterop);
#endif

        extension(DisplayInformation)
        {
            /// <summary>
            /// Retrieves a <see cref="DisplayInformation"/> object for the specified window.
            /// </summary>
            /// <param name="window">The handle to the window (an <c>HWND</c>).</param>
            /// <returns>A new <see cref="DisplayInformation"/> object for the specified window.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="DisplayInformation"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/windows.graphics.display.interop/nf-windows-graphics-display-interop-idisplayinformationstaticsinterop-getforwindow"/>
            public static DisplayInformation GetForWindow(nint window)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IDisplayInformationStaticsInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    window: window,
                    riid: in global::ABI.InterfaceIIDs.IID_Windows_Graphics_Display_IDisplayInformation);
#endif
            }

            /// <summary>
            /// Retrieves a <see cref="DisplayInformation"/> object for the specified monitor.
            /// </summary>
            /// <param name="monitor">The handle to the monitor (an <c>HMONITOR</c>).</param>
            /// <returns>A new <see cref="DisplayInformation"/> object for the specified monitor.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="DisplayInformation"/> object.</exception>
            /// <see href="https://learn.microsoft.com/windows/win32/api/windows.graphics.display.interop/nf-windows-graphics-display-interop-idisplayinformationstaticsinterop-getformonitor"/>
            public static DisplayInformation GetForMonitor(nint monitor)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IDisplayInformationStaticsInteropMethods.GetForMonitor(
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
    /// <summary>
    /// Extensions for <see cref="DataTransferManager"/>.
    /// </summary>
    [global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.10240.0")]
    public static class DataTransferManagerExtensions
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>The cached <see cref="DataTransferManager"/> activation factory, as <c>IDataTransferManagerInterop</c>.</summary>
        private static readonly WindowsRuntimeObjectReference objectReference = WindowsRuntimeActivationFactory.GetActivationFactory(
            runtimeClassName: "Windows.ApplicationModel.DataTransfer.DataTransferManager",
            iid: in IDataTransferManagerInteropMethods.IID);
#endif

        extension(DataTransferManager)
        {
            /// <summary>
            /// Gets a <see cref="DataTransferManager"/> object for the window of the active application.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <returns>The resulting <see cref="DataTransferManager"/> object.</returns>
            /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="DataTransferManager"/> object.</exception>
            public static global::Windows.ApplicationModel.DataTransfer.DataTransferManager GetForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                return global::ABI.WindowsRuntime.Internal.IDataTransferManagerInteropMethods.GetForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow,
                    riid: in global::ABI.InterfaceIIDs.IDataTransferManager);
#endif
            }

            /// <summary>
            /// Programmatically initiates the share UI for the specified window.
            /// </summary>
            /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
            /// <exception cref="Exception">Thrown if failed to display the share UI.</exception>
            public static void ShowShareUIForWindow(nint appWindow)
            {
#if CSWINRT_REFERENCE_PROJECTION
                throw null;
#else
                global::ABI.WindowsRuntime.Internal.IDataTransferManagerInteropMethods.ShowShareUIForWindow(
                    thisReference: objectReference,
                    appWindow: appWindow);
#endif
            }
        }
    }
}

namespace ABI.WindowsRuntime.Internal
{
    using HRESULT = int;

#if !CSWINRT_REFERENCE_PROJECTION
    /// <summary>
    /// ABI methods for <c>IDataTransferManagerInterop</c>.
    /// </summary>
    internal static class IDataTransferManagerInteropMethods
    {
        /// <summary>The IID of <c>IDataTransferManagerInterop</c> (<c>3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8</c>).</summary>
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

        /// <summary>
        /// Gets a <see cref="global::Windows.ApplicationModel.DataTransfer.DataTransferManager"/> object for the window of the active application.
        /// </summary>
        /// <param name="thisReference">The activation factory object reference.</param>
        /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
        /// <param name="riid">The IID of the <see cref="global::Windows.ApplicationModel.DataTransfer.DataTransferManager"/> class.</param>
        /// <returns>The resulting <see cref="global::Windows.ApplicationModel.DataTransfer.DataTransferManager"/> object.</returns>
        /// <exception cref="Exception">Thrown if failed to retrieve the <see cref="global::Windows.ApplicationModel.DataTransfer.DataTransferManager"/> object.</exception>
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
                fixed (Guid* iidPtr = &riid)
                {
                    HRESULT hresult = ((delegate* unmanaged[MemberFunction]<void*, nint, Guid*, void**, HRESULT>)(*(void***)thisPtr)[3])(thisPtr, appWindow, iidPtr, &result);

                    RestrictedErrorInfo.ThrowExceptionForHR(hresult);
                }

                return global::ABI.Windows.ApplicationModel.DataTransfer.DataTransferManagerMarshaller.ConvertToManaged(result);
            }
            finally
            {
                WindowsRuntimeUnknownMarshaller.Free(result);
            }
        }

        /// <summary>
        /// Programmatically initiates the share UI for the specified window.
        /// </summary>
        /// <param name="thisReference">The activation factory object reference.</param>
        /// <param name="appWindow">The handle to the window of the active application (an <c>HWND</c>).</param>
        /// <exception cref="Exception">Thrown if failed to display the share UI.</exception>
        public static unsafe void ShowShareUIForWindow(WindowsRuntimeObjectReference thisReference, nint appWindow)
        {
            using WindowsRuntimeObjectReferenceValue activationFactoryValue = thisReference.AsValue();

            void* thisPtr = activationFactoryValue.GetThisPtrUnsafe();

            HRESULT hresult = ((delegate* unmanaged[MemberFunction]<void*, nint, HRESULT>)(*(void***)thisPtr)[4])(thisPtr, appWindow);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }
#endif
}
