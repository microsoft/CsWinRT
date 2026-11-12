// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Identifies an assembly for an authored Windows Runtime component written in C#, which will produce its own
/// Windows Runtime metadata file (.winmd). This assembly is meant to be consumed by native code, either via a
/// native host (WinRT.Host.dll), or published to a native binary via Native AOT.
/// </summary>
/// <remarks>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly.
/// </remarks>
/// <seealso cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeComponentAssemblyObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeComponentAssemblyObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class WindowsRuntimeComponentAssemblyAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeComponentAssemblyAttribute"/> instance.
    /// </summary>
    public WindowsRuntimeComponentAssemblyAttribute()
    {
    }
}
