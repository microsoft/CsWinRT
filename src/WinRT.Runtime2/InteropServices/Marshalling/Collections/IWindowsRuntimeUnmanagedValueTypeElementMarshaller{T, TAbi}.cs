// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to native.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
/// <typeparam name="TAbi">The ABI type for type <typeparamref name="T"/>.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>
    where T : unmanaged
    where TAbi : unmanaged
{
    /// <summary>
    /// Marshals an unmanaged Windows Runtime value type to its native representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract TAbi ConvertToUnmanaged(T value);
}
