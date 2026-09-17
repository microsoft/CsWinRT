// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to and from native.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
[WindowsRuntimeImplementationOnlyMember]
public unsafe interface IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>
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

    /// <summary>
    /// Disposes resources associated with an unmanaged value.
    /// </summary>
    /// <param name="value">The unmanaged value to dispose.</param>
    static abstract void Dispose(void* value);
}
