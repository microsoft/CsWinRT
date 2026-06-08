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
[Guid("DDB0472D-C911-4A1F-86D9-DC3D71A95F5A")]
public interface ISystemMediaTransportControlsInterop
{
    /// <summary>
    /// Gets the <see cref="SystemMediaTransportControls"/> associated with the specified window.
    /// </summary>
    SystemMediaTransportControls GetForWindow(
        HWND appWindow,
        ref Guid riid);
}
