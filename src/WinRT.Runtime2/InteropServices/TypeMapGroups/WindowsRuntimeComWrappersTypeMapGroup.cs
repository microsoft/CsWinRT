// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The type map group placeholder for all Windows Runtime types, and user-defined types, that should go through <see cref="System.Runtime.InteropServices.ComWrappers"/>.
/// </summary>
/// <remarks>
/// This type is only meant to be used as type map group for <see cref="System.Runtime.InteropServices.TypeMapping"/> APIs.
/// </remarks>
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeTypeMapGroupObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeTypeMapGroupObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public abstract class WindowsRuntimeComWrappersTypeMapGroup
{
    /// <summary>
    /// This type should never be instantiated (it just can't be static because it needs to be used as a type argument).
    /// </summary>
    private WindowsRuntimeComWrappersTypeMapGroup()
    {
    }
}
