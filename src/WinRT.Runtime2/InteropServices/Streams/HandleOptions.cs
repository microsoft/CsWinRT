// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Specifies the handle options for a storage item.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_options"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
[Flags]
public enum HandleOptions : uint
{
    /// <summary>No options.</summary>
    None = 0,

    /// <summary>Open requiring oplock.</summary>
    OpenRequiringOplock = 0x40000,

    /// <summary>Delete on close.</summary>
    DeleteOnClose = 0x4000000,

    /// <summary>Sequential scan.</summary>
    SequentialScan = 0x8000000,

    /// <summary>Random access.</summary>
    RandomAccess = 0x10000000,

    /// <summary>No buffering.</summary>
    NoBuffering = 0x20000000,

    /// <summary>Overlapped.</summary>
    Overlapped = 0x40000000,

    /// <summary>Write through.</summary>
    WriteThrough = 0x80000000
}
