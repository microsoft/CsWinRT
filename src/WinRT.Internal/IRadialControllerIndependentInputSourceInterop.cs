// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.UI.Input.Core;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for creating a <see cref="RadialControllerIndependentInputSource"/> bound to a Win32 window.
/// </summary>
[ProjectionInternal]
[Guid("3D577EFF-4CEE-11E6-B535-001BDC06AB3B")]
public interface IRadialControllerIndependentInputSourceInterop
{
    /// <summary>
    /// Creates a <see cref="RadialControllerIndependentInputSource"/> for the specified window.
    /// </summary>
    RadialControllerIndependentInputSource CreateForWindow(
        HWND hwnd,
        ref Guid riid);
}
