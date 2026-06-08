// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics.Printing;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving a <see cref="PrintManager"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/printmanagerinterop/">PrintManagerInterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("C5435A42-8D43-4E7B-A68A-EF311E392087")]
public interface IPrintManagerInterop
{
    /// <summary>
    /// Gets the <see cref="PrintManager"/> associated with the specified window.
    /// </summary>
    PrintManager GetForWindow(
        HWND appWindow,
        ref Guid riid);

    /// <summary>
    /// Shows the print UI for the specified window.
    /// </summary>
    IAsyncOperation<bool> ShowPrintUIForWindowAsync(
        HWND appWindow,
        ref Guid riid);
}
