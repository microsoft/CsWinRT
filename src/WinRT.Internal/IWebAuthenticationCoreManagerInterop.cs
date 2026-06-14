// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for requesting web authentication tokens bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/webauthenticationcoremanagerinterop/">WebAuthenticationCoreManagerInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("F4B8E804-811E-4436-B69C-44CB67B72084")]
public interface IWebAuthenticationCoreManagerInterop
{
    /// <summary>
    /// Asynchronously requests a token for the specified window.
    /// </summary>
    IAsyncOperation<WebTokenRequestResult> RequestTokenForWindowAsync(
        HWND appWindow,
        WebTokenRequest request,
        ref Guid riid);

    /// <summary>
    /// Asynchronously requests a token for the specified window using the specified web account.
    /// </summary>
    IAsyncOperation<WebTokenRequestResult> RequestTokenWithWebAccountForWindowAsync(
        HWND appWindow,
        WebTokenRequest request,
        WebAccount webAccount,
        ref Guid riid);
}
