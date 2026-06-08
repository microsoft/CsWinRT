// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.UI.Input.Spatial;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving a <see cref="SpatialInteractionManager"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/spatialinteractionmanagerinterop/">SpatialInteractionManagerInterop.idl</see>.
/// This interop interface is duplicated by <c>IHolographicSpaceInterop</c>, which has the same IID.
/// </remarks>
[ProjectionInternal]
[Guid("5C4EE536-6A98-4B86-A170-587013D6FD4B")]
public interface ISpatialInteractionManagerInterop
{
    /// <summary>
    /// Gets the <see cref="SpatialInteractionManager"/> associated with the specified window.
    /// </summary>
    SpatialInteractionManager GetForWindow(
        HWND window,
        ref Guid riid);
}
