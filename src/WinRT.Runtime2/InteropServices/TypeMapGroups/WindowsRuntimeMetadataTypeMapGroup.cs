// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The type map group placeholder for all Windows Runtime types that need to support <see cref="Type"/> marshalling.
/// </summary>
/// <remarks>
/// This type is only meant to be used as type map group for <see cref="System.Runtime.InteropServices.TypeMapping"/> APIs.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeMetadataTypeMapGroup
{
    /// <summary>
    /// This type should never be instantiated (it just can't be static because it needs to be used as a type argument).
    /// </summary>
    private WindowsRuntimeMetadataTypeMapGroup()
    {
    }
}