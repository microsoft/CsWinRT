// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to native.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
[WindowsRuntimeImplementationOnlyMember]
public interface IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>
{
    /// <summary>
    /// Marshals a <see cref="KeyValuePair{TKey, TValue}"/> type to its native Windows Runtime representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(KeyValuePair<TKey, TValue> value);
}
