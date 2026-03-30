// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

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
public sealed class WindowsRuntimeComponentAssemblyAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeComponentAssemblyAttribute"/> instance.
    /// </summary>
    public WindowsRuntimeComponentAssemblyAttribute()
    {
    }
}
#endif
