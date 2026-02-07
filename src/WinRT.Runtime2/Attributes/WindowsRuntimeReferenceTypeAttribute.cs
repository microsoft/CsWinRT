// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Indicates the reference type associated to a given Windows Runtime value type.
/// </summary>
/// <remarks>This attribute is only needed for the <see cref="Type"/> marshalling infrastructure.</remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum,
    AllowMultiple = false,
    Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeReferenceTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeReferenceTypeAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="referenceType">The reference type (a constructed <see cref="Nullable{T}"/> type) for the annotated type.</param>
    public WindowsRuntimeReferenceTypeAttribute(Type referenceType)
    {
        ReferenceType = referenceType;
    }

    /// <summary>
    /// Gets the reference type (a constructed <see cref="Nullable{T}"/> type) for the annotated type.
    /// </summary>
    public Type ReferenceType { get; }
}