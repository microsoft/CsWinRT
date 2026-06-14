// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.UI.Input;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving a <see cref="RadialControllerConfiguration"/> bound to a Win32 window.
/// </summary>
[ProjectionInternal]
[Guid("787CDAAC-3186-476D-87E4-B9374A7B9970")]
public interface IRadialControllerConfigurationInterop
{
    /// <summary>
    /// Gets the <see cref="RadialControllerConfiguration"/> associated with the specified window.
    /// </summary>
    RadialControllerConfiguration GetForWindow(
        HWND hwnd,
        ref Guid riid);
}
