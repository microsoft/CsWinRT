// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Identifies an assembly as containing generated Windows Runtime APIs from a given Windows Runtime metadata file
/// (.winmd). The annotated assembly can either be a reference assembly, which contains metadata but no executable code
/// (analogous to <see cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>), or an implementation assembly
/// for Windows Runtime projections consumed directly from a local project reference.
/// </summary>
/// <remarks>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly. Unlike most other CsWinRT
/// implementation details, it is not stripped from the <c>WinRT.Runtime.dll</c> reference assembly: it is applied (via
/// <c>[assembly: WindowsRuntimeReferenceAssembly]</c>) to the reference projection assemblies that ship in Windows Runtime
/// projection NuGet packages, so it must remain resolvable when those assemblies are consumed.
/// </remarks>
/// <seealso cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeReferenceAssemblyObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeReferenceAssemblyObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class WindowsRuntimeReferenceAssemblyAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeReferenceAssemblyAttribute"/> instance.
    /// </summary>
    public WindowsRuntimeReferenceAssemblyAttribute()
    {
    }
}
