// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Represents a reference to a fast-pass <c>HSTRING</c> value (passed without copying).
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe struct HStringReference
{
    /// <summary>
    /// The underlying header for the <c>HSTRING</c> reference.
    /// </summary>
    internal HSTRING_HEADER _header;

    /// <summary>
    /// The fast-pass <c>HSTRING</c> value.
    /// </summary>
    internal HSTRING _hstring;

    /// <summary>
    /// Gets the fast-pass <c>HSTRING</c> value for this reference.
    /// </summary>
    /// <remarks>
    /// It is not valid to escape this value outside of the scope of the current <see cref="HStringReference"/> instance.
    /// </remarks>
    public readonly HSTRING HString => _hstring;
}