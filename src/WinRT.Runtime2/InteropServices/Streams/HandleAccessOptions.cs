// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Specifies the access options for a storage item handle.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/ne-windowsstoragecom-handle_access_options"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
[Flags]
public enum HandleAccessOptions : uint
{
    /// <summary>No access options.</summary>
    None = 0,

    /// <summary>Read attributes access.</summary>
    ReadAttributes = 0x80,

    /// <summary>Read access.</summary>
    Read = Synchronize | ReadControl | ReadAttributes | FileReadEA | FileReadData,

    /// <summary>Write access.</summary>
    Write = Synchronize | ReadControl | FileWriteAttributes | FileWriteEA | FileAppendData | FileWriteData,

    /// <summary>Delete access.</summary>
    Delete = 0x10000,

    /// <summary>Read control access.</summary>
    ReadControl = 0x00020000,

    /// <summary>Synchronize access.</summary>
    Synchronize = 0x00100000,

    /// <summary>File read data access.</summary>
    FileReadData = 0x00000001,

    /// <summary>File write data access.</summary>
    FileWriteData = 0x00000002,

    /// <summary>File append data access.</summary>
    FileAppendData = 0x00000004,

    /// <summary>File read extended attributes access.</summary>
    FileReadEA = 0x00000008,

    /// <summary>File write extended attributes access.</summary>
    FileWriteEA = 0x00000010,

    /// <summary>File execute access.</summary>
    FileExecute = 0x00000020,

    /// <summary>File write attributes access.</summary>
    FileWriteAttributes = 0x00000100
}
