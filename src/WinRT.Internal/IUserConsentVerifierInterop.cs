// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Security.Credentials.UI;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for requesting user consent verification bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/userconsentverifierinterop/">UserConsentVerifierInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("39E050C3-4E74-441A-8DC0-B81104DF949C")]
public interface IUserConsentVerifierInterop
{
    /// <summary>
    /// Asynchronously requests user consent verification for the specified window.
    /// </summary>
    IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(
        HWND appWindow,
        string message,
        ref Guid riid);
}
