// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime;

/// <summary>
/// Identifies an abstract base class as the authoring surface for a Windows Runtime class declared in existing
/// Windows Runtime metadata (.winmd), and names the projected class it stands for.
/// </summary>
/// <remarks>
/// <para>
/// CsWinRT emits this attribute on the abstract base classes it generates when a projection is built with
/// <c>CsWinRTImplementWinMDTypes</c>. A type deriving from such a base is an implementation of the referenced
/// Windows Runtime class, so its COM Callable Wrapper reports that class name rather than the name of the
/// deriving type. CsWinRT tooling also uses this attribute to recognize the generated bases.
/// </para>
/// <para>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly. Unlike most
/// other CsWinRT implementation details, it is not stripped from the <c>WinRT.Runtime.dll</c> reference
/// assembly, because the reference projections that carry these base classes are compiled against it.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[Obsolete(
    WindowsRuntimeConstants.WindowsRuntimeImplementableClassObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.WindowsRuntimeImplementableClassObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public sealed class WindowsRuntimeImplementableClassAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeImplementableClassAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The projected Windows Runtime class type that the annotated base class allows implementing.</param>
    public WindowsRuntimeImplementableClassAttribute(Type runtimeClassType)
    {
        RuntimeClassType = runtimeClassType;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type that the annotated base class allows implementing.
    /// </summary>
    public Type RuntimeClassType { get; }
}
