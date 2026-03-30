// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling implementations to support <see cref="WindowsRuntimeManagedValueTypeArrayMarshaller{T, TAbi}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
/// <typeparam name="TAbi">The ABI type for type <typeparamref name="T"/>.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>
    where T : struct
    where TAbi : unmanaged
{
    /// <summary>
    /// Marshals an unmanaged Windows Runtime value type to its native representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract TAbi ConvertToUnmanaged(T value);

    /// <summary>
    /// Marshals a native Windows Runtime value type to its managed representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled managed value.</returns>
    static abstract T ConvertToManaged(TAbi value);

    /// <summary>
    /// Disposes resources associated with an unmanaged value.
    /// </summary>
    /// <param name="value">The unmanaged value to dispose.</param>
    static abstract void Dispose(TAbi value);
}
#endif