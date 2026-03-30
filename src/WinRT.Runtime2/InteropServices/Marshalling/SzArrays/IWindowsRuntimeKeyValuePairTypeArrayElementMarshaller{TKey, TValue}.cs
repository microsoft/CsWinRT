// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling implementations to support <see cref="WindowsRuntimeKeyValuePairTypeArrayMarshaller{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe interface IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>
{
    /// <summary>
    /// Marshals a <see cref="KeyValuePair{TKey, TValue}"/> type to its native Windows Runtime representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(KeyValuePair<TKey, TValue> value);

    /// <summary>
    /// Marshals a native Windows Runtime <see cref="KeyValuePair{TKey, TValue}"/> type to its managed representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled managed value.</returns>
    static abstract KeyValuePair<TKey, TValue> ConvertToManaged(void* value);
}
#endif