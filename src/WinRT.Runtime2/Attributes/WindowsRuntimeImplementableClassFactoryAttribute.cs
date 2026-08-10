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
/// CsWinRT emits this on the abstract factory base classes it generates when a projection is built with
/// <c>CsWinRTImplementWinMDTypes</c>. The named class is what activation requests are keyed by.
/// </para>
/// <para>
/// Unlike <see cref="WindowsRuntimeImplementableClassAttribute"/>, it does not affect the runtime class name
/// the COM Callable Wrapper reports: an activation factory is not an instance of the class it activates, so
/// it keeps reporting its own most derived Windows Runtime interface.
/// </para>
/// <para>
/// It is not meant to be used directly. Unlike most other CsWinRT implementation details, it is not stripped
/// from the <c>WinRT.Runtime.dll</c> reference assembly, because the reference projections carrying these
/// base classes are compiled against it.
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

    /// <summary>
    /// Gets whether the only way to activate the class is the parameterless <c>IActivationFactory.ActivateInstance</c>,
    /// i.e. the class declares no factory, statics or composable interfaces.
    /// </summary>
    /// <remarks>
    /// CsWinRT can supply the factory for such a class on the author's behalf, as implementing it amounts to
    /// constructing the implementation type. Any other shape has members only the author can implement.
    /// </remarks>
    public bool HasDefaultActivationOnly { get; init; }
}
