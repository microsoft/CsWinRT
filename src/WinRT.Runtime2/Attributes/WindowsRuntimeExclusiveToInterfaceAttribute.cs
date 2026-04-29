// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Indicates that a given interface is exclusive to a projected Windows Runtime class type. This attribute is applied
/// to a centralized lookup type to associate a runtime class type with its exclusive interface, rather than being
/// placed on the interface type itself. This allows the interface type reference to be trimmed away when not needed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeExclusiveToInterfaceAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeExclusiveToInterfaceAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The projected Windows Runtime class type that the interface is exclusive to.</param>
    /// <param name="interfaceType">The type of the exclusive interface for the specified Windows Runtime class type.</param>
    public WindowsRuntimeExclusiveToInterfaceAttribute(Type runtimeClassType, Type interfaceType)
    {
        RuntimeClassType = runtimeClassType;
        InterfaceType = interfaceType;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type that the interface is exclusive to.
    /// </summary>
    public Type RuntimeClassType { get; }

    /// <summary>
    /// Gets the type of the exclusive interface for the specified Windows Runtime class type.
    /// </summary>
    public Type InterfaceType { get; }
}