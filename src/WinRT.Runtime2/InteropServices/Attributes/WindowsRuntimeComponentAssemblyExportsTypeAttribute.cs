// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates which type contains the managed <c>GetActivationFactory</c> method to invoke for authoring scenarios. This
/// attribute is only meant to be used within an assembly annotated with <see cref="WindowsRuntimeComponentAssemblyAttribute"/>.
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
public sealed class WindowsRuntimeComponentAssemblyExportsTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeComponentAssemblyExportsTypeAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exportsType">The type that contains the managed <c>GetActivationFactory</c> method to invoke for authoring scenarios.</param>
    public WindowsRuntimeComponentAssemblyExportsTypeAttribute(Type exportsType)
    {
        ExportsType = exportsType;
    }

    /// <summary>
    /// Gets the type that contains the managed <c>GetActivationFactory</c> method to invoke for authoring scenarios.
    /// </summary>
    public Type ExportsType { get; }
}
