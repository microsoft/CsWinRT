// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// An interface for marshalling collection elements to and from native.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
[WindowsRuntimeImplementationOnlyMember]
public unsafe interface IWindowsRuntimeReferenceTypeElementMarshaller<T>
    where T : class
{
    /// <summary>
    /// Marshals a Windows Runtime object to a <see cref="WindowsRuntimeObjectReferenceValue"/> instance.
    /// </summary>
    /// <param name="value">The input object to marshal.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> instance for <paramref name="value"/>.</returns>
    static abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(T? value);

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object.
    /// </summary>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed object.</returns>
    static abstract T? ConvertToManaged(void* value);

    /// <summary>
    /// Disposes resources associated with an unmanaged value.
    /// </summary>
    /// <param name="value">The unmanaged value to dispose.</param>
    static abstract void Dispose(void* value);
}
