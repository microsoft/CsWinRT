// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Identifies an assembly as a Windows Runtime reference assembly, which contains metadata but no executable code.
/// This is analogous to <see cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>, but specifically
/// for reference assemblies for generated Windows Runtime projections for a given Windows Runtime metadata file.
/// </summary>
/// <remarks>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly.
/// </remarks>
/// <seealso cref="System.Runtime.CompilerServices.ReferenceAssemblyAttribute"/>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeReferenceAssemblyAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeReferenceAssemblyAttribute"/> instance.
    /// </summary>
    public WindowsRuntimeReferenceAssemblyAttribute()
    {
    }
}
