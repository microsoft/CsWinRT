// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.ComponentModel;
#endif

namespace WindowsRuntime;

/// <summary>
/// Identifies an abstract base class as the authoring surface for the activation factory of a Windows Runtime
/// class declared in existing Windows Runtime metadata (.winmd), and names the projected class it activates.
/// </summary>
/// <remarks>
/// <para>
/// CsWinRT emits this attribute on the abstract factory base classes it generates when a projection is built
/// with <c>CsWinRTImplementWinMDTypes</c>. It identifies the class a derived factory activates, which is the
/// name activation requests are keyed by.
/// </para>
/// <para>
/// Unlike <see cref="WindowsRuntimeImplementableClassAttribute"/>, this attribute does not affect the runtime
/// class name reported by the COM Callable Wrapper: an activation factory is not an instance of the class it
/// activates, so it keeps reporting its own most derived Windows Runtime interface.
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
public sealed class WindowsRuntimeImplementableClassFactoryAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeImplementableClassFactoryAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The projected Windows Runtime class type that the annotated base class activates.</param>
    public WindowsRuntimeImplementableClassFactoryAttribute(Type runtimeClassType)
    {
        RuntimeClassType = runtimeClassType;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type that the annotated base class activates.
    /// </summary>
    public Type RuntimeClassType { get; }
}
