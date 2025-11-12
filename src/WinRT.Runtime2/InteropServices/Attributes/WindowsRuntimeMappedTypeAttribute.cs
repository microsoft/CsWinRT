// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates a mapped type for a Windows Runtime type projection (ie. a metadata provider type).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WindowsRuntimeMappedTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMappedTypeAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="publicType">The public type associated with the current instance (ie. the type that would be used directly by developers).</param>
    public WindowsRuntimeMappedTypeAttribute(Type publicType)
    {
        PublicType = publicType;
    }

    /// <summary>
    /// Gets the public type associated with the current instance (ie. the type that would be used directly by developers).
    /// </summary>
    public Type PublicType { get; }
}
