// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving a <see cref="CoreDragDropManager"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/dragdropinterop/">dragdropinterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("5AD8CBA7-4C01-4DAC-9074-827894292D63")]
public interface IDragDropManagerInterop
{
    /// <summary>
    /// Gets the <see cref="CoreDragDropManager"/> associated with the specified window.
    /// </summary>
    CoreDragDropManager GetForWindow(
        HWND hwnd,
        ref Guid riid);
}
