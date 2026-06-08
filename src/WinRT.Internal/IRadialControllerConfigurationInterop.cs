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
[Guid("787cdaac-3186-476d-87e4-b9374a7b9970")]
public interface IRadialControllerConfigurationInterop
{
    /// <summary>
    /// Gets the <see cref="RadialControllerConfiguration"/> associated with the specified window.
    /// </summary>
    RadialControllerConfiguration GetForWindow(
        HWND hwnd,
        ref Guid riid);
}
