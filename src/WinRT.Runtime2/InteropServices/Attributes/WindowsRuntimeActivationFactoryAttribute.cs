// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates the authored Windows Runtime class type that a given activation factory is for.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeActivationFactoryAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeActivationFactoryAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The authored Windows Runtime class type that the annotated activation factory is for.</param>
    public WindowsRuntimeActivationFactoryAttribute(Type runtimeClassType)
    {
        RuntimeClassType = runtimeClassType;
    }

    /// <summary>
    /// Gets the authored Windows Runtime class type that the annotated activation factory is for.
    /// </summary>
    public Type RuntimeClassType { get; }
}
#endif