// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.UI.Input;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for creating a <see cref="RadialController"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/radialcontrollerinterop/">RadialControllerInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("1B0535C9-57AD-45C1-9D79-AD5C34360513")]
public interface IRadialControllerInterop
{
    /// <summary>
    /// Creates a <see cref="RadialController"/> for the specified window.
    /// </summary>
    RadialController CreateForWindow(
        HWND hwnd,
        ref Guid riid);
}
