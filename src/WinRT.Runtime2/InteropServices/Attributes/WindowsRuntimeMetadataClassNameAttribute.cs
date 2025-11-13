// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates the runtime class name to use for types exposed to the Windows Runtime.
/// </summary>
/// <remarks>
/// This is conceptually the same as <see cref="WindowsRuntimeClassNameAttribute"/>, but only used by generated projections,
/// so that the public attribute can have its targets restricted to only the scenarios that would be valid for user types.
/// </remarks>
/// <see cref="WindowsRuntimeClassNameAttribute"/>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeMetadataClassNameAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMetadataClassNameAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name to use.</param>
    public WindowsRuntimeMetadataClassNameAttribute(string runtimeClassName)
    {
        RuntimeClassName = runtimeClassName;
    }

    /// <summary>
    /// Gets the runtime class name for the current instance.
    /// </summary>
    public string RuntimeClassName { get; }
}
