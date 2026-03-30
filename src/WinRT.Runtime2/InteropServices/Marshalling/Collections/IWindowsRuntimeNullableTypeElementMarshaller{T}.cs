// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to native.
/// </summary>
/// <typeparam name="T">The underlying value type of the nullable type.</typeparam>
public interface IWindowsRuntimeNullableTypeElementMarshaller<T>
    where T : struct
{
    /// <summary>
    /// Marshals a <see cref="Nullable{T}"/> type to its native Windows Runtime representation.
    /// </summary>
    /// <param name="value">The input value to marshal.</param>
    /// <returns>The marshalled native value.</returns>
    static abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(T? value);
}
#endif