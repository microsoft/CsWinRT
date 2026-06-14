// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Display;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving <see cref="DisplayInformation"/> bound to a Win32 window or monitor handle.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/windows.graphics.display.interop/">WindowsGraphicsDisplayInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("7449121C-382B-4705-8DA7-A795BA482013")]
public interface IDisplayInformationStaticsInterop
{
    /// <summary>
    /// Gets the <see cref="DisplayInformation"/> associated with the specified window.
    /// </summary>
    DisplayInformation GetForWindow(
        HWND window,
        ref Guid riid);

    /// <summary>
    /// Gets the <see cref="DisplayInformation"/> associated with the specified monitor.
    /// </summary>
    DisplayInformation GetForMonitor(
        HWND monitor,
        ref Guid riid);
}
