// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to native.
/// </summary>
/// <typeparam name="T">The underlying value type of the nullable type.</typeparam>
[WindowsRuntimeImplementationOnlyMember]
public unsafe interface IWindowsRuntimeNullableTypeElementMarshaller<T>
    where T : struct
{
    /// <summary>
    /// Marshals a <see cref="Nullable{T}"/> type to its native Windows Runtime representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(T? value);

    /// <summary>
    /// Converts an unmanaged nullable value to its managed representation.
    /// </summary>
    static abstract T? ConvertToManaged(void* value);

    /// <summary>
    /// Releases an unmanaged nullable value.
    /// </summary>
    static abstract void Dispose(void* value);
}
