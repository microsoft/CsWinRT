// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Indicates the source Windows Runtime metadata file (.winmd) that a given projected type is from.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMetadataAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the source Windows Runtime metadata file (.winmd) that the current projected type is from.</param>
    public WindowsRuntimeMetadataAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the source Windows Runtime metadata file (.winmd) that the current projected type is from.
    /// </summary>
    public string Name { get; }
}