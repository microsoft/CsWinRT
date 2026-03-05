// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Specifies the sharing options for a storage item handle.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_sharing_options"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
[Flags]
public enum HandleSharingOptions : uint
{
    /// <summary>No sharing.</summary>
    ShareNone = 0,

    /// <summary>Share read access.</summary>
    ShareRead = 0x1,

    /// <summary>Share write access.</summary>
    ShareWrite = 0x2,

    /// <summary>Share delete access.</summary>
    ShareDelete = 0x4
}
