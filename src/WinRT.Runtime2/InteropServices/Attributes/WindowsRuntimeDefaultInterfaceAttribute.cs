// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates the default interface for a projected Windows Runtime class type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeDefaultInterfaceAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeDefaultInterfaceAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="interfaceType">The type of the default interface for the annotated Windows Runtime class type.</param>
    public WindowsRuntimeDefaultInterfaceAttribute(Type interfaceType)
    {
        InterfaceType = interfaceType;
    }

    /// <summary>
    /// Gets the type of the default interface for the annotated Windows Runtime class type.
    /// </summary>
    public Type InterfaceType { get; }
}
