// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving <see cref="UIViewSettings"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/uiviewsettingsinterop/">UIViewSettingsInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("3694DBF9-8F68-44BE-8FF5-195C98EDE8A6")]
public interface IUIViewSettingsInterop
{
    /// <summary>
    /// Gets the <see cref="UIViewSettings"/> associated with the specified window.
    /// </summary>
    UIViewSettings GetForWindow(
        HWND hwnd,
        ref Guid riid);
}
