// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling implementations to support <see cref="WindowsRuntimeNullableTypeArrayMarshaller{T}"/>.
/// </summary>
/// <typeparam name="T">The underlying value type of the nullable type.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe interface IWindowsRuntimeNullableTypeArrayElementMarshaller<T>
    where T : struct
{
    /// <summary>
    /// Marshals a <see cref="Nullable{T}"/> value to its native Windows Runtime representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(T? value);

    /// <summary>
    /// Marshals a native Windows Runtime <see cref="Nullable{T}"/> value to its managed representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled managed value.</returns>
    static abstract T? ConvertToManaged(void* value);
}
#endif