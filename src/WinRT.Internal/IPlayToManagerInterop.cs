// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Media.PlayTo;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving a <see cref="PlayToManager"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/playtomanagerinterop/">PlayToManagerInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("24394699-1F2C-4EB3-8CD7-0EC1DA42A540")]
public interface IPlayToManagerInterop
{
    /// <summary>
    /// Gets the <see cref="PlayToManager"/> associated with the specified window.
    /// </summary>
    PlayToManager GetForWindow(
        HWND appWindow,
        ref Guid riid);

    /// <summary>
    /// Shows the Play To UI for the specified window.
    /// </summary>
    void ShowPlayToUIForWindow(
        HWND appWindow);
}
