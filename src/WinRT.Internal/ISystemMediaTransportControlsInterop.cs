// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Media;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving a <see cref="SystemMediaTransportControls"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/systemmediatransportcontrolsinterop/">SystemMediaTransportControlsInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("ddb0472d-c911-4a1f-86d9-dc3d71a95f5a")]
public interface ISystemMediaTransportControlsInterop
{
    /// <summary>
    /// Gets the <see cref="SystemMediaTransportControls"/> associated with the specified window.
    /// </summary>
    SystemMediaTransportControls GetForWindow(
        HWND appWindow,
        ref Guid riid);
}
