// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving an <see cref="InputPane"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/inputpaneinterop/">inputpaneinterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("75CF2C57-9195-4931-8332-F0B409E916AF")]
public interface IInputPaneInterop
{
    /// <summary>
    /// Gets the <see cref="InputPane"/> associated with the specified window.
    /// </summary>
    InputPane GetForWindow(
        HWND appWindow,
        ref Guid riid);
}
