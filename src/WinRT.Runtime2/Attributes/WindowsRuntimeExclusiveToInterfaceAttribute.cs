// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Indicates the projected Windows Runtime class type that a given interface is exclusive to.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeExclusiveToInterfaceAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeExclusiveToInterfaceAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="interfaceType">The projected Windows Runtime class type that the annotated interface is exclusive to.</param>
    public WindowsRuntimeExclusiveToInterfaceAttribute(Type interfaceType)
    {
        RuntimeClassType = interfaceType;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type that the annotated interface is exclusive to.
    /// </summary>
    public Type RuntimeClassType { get; }
}