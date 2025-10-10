// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Identifies an assembly as containing generated Windows Runtime APIs from a given Windows Runtime metadata file
/// (.winmd). The annotated assembly can either be a reference assembly, which contains metadata but no executable code
/// (analogous to <see cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>), or an implementation assembly
/// for Windows Runtime projections consumed directly from a local project reference.
/// </summary>
/// <remarks>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly.
/// </remarks>
/// <seealso cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeReferenceAssemblyAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeReferenceAssemblyAttribute"/> instance.
    /// </summary>
    public WindowsRuntimeReferenceAssemblyAttribute()
    {
    }
}
