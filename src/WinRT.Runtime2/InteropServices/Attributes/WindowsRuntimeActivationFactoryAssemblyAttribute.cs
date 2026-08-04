// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Identifies an assembly that declares activation factories for Windows Runtime classes it implements, so that
/// its activation entry point participates in the merged activation performed by <c>WinRT.Component.dll</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is narrower than <see cref="WindowsRuntimeComponentAssemblyAttribute"/>: it only states that the assembly
/// has an <c>ABI.&lt;AssemblyName&gt;.ManagedExports</c> to include in the merged activation chain. It does not
/// imply that the assembly produces its own Windows Runtime metadata file (.winmd), nor that its types are
/// projected into <c>WinRT.Component.dll</c>. An assembly implementing classes declared in existing metadata
/// carries only this attribute; an authored component carries both.
/// </para>
/// <para>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeComponentAssemblyObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeComponentAssemblyObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class WindowsRuntimeActivationFactoryAssemblyAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeActivationFactoryAssemblyAttribute"/> instance.
    /// </summary>
    public WindowsRuntimeActivationFactoryAssemblyAttribute()
    {
    }
}
