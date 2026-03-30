// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to native.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
/// <typeparam name="TAbi">The ABI type for type <typeparamref name="T"/>.</typeparam>
public interface IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>
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
    /// Disposes resources associated with an unmanaged value.
    /// </summary>
    /// <param name="value">The unmanaged value to dispose.</param>
    static abstract void Dispose(TAbi value);
}
#endif
