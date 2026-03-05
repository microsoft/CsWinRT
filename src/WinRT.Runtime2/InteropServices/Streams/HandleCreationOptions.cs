// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Specifies the creation options for a storage item handle.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_creation_options"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public enum HandleCreationOptions : uint
{
    /// <summary>Creates a new item. Fails if the item already exists.</summary>
    CreateNew = 0x1,

    /// <summary>Creates a new item. Overwrites an existing item.</summary>
    CreateAlways = 0x2,

    /// <summary>Opens an existing item. Fails if the item does not exist.</summary>
    OpenExisting = 0x3,

    /// <summary>Opens an existing item, or creates a new item if one does not exist.</summary>
    OpenAlways = 0x4,

    /// <summary>Opens an existing item and truncates it. Fails if the item does not exist.</summary>
    TruncateExisting = 0x5
}
