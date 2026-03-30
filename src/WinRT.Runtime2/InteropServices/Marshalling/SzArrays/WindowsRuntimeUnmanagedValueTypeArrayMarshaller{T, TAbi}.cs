// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of unmanaged Windows Runtime types.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
/// <typeparam name="TAbi">The ABI type for type <typeparamref name="T"/>.</typeparam>
public static unsafe class WindowsRuntimeUnmanagedValueTypeArrayMarshaller<T, TAbi>
    where T : unmanaged
    where TAbi : unmanaged
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void ConvertToUnmanaged<TElementMarshaller>(ReadOnlySpan<T> source, out uint size, out TAbi* array)
        where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        TAbi* destination = (TAbi*)Marshal.AllocCoTaskMem(sizeof(T) * source.Length);

        try
        {
            // Marshal all array elements with the provided element marshaller.
            // We don't need to guard each call, as the ABI type is unmanaged.
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // If marshalling any element failed, release the allocated array to avoid
            // leaking. This shouldn't really happen with unmanaged value type, since
            // the conversions should be pretty trivial, but leave it just in case.
            Marshal.FreeCoTaskMem((nint)destination);

            throw;
        }

        size = (uint)source.Length;
        array = destination;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static T[] ConvertToManaged<TElementMarshaller>(uint size, TAbi* value)
        where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        T[] array = GC.AllocateUninitializedArray<T>((int)size);

        for (int i = 0; i < size; i++)
        {
            array[i] = TElementMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToUnmanaged<TElementMarshaller>(ReadOnlySpan<T> source, uint size, TAbi* destination)
        where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(source.Length, size);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(destination);

        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToManaged<TElementMarshaller>(uint size, TAbi* source, Span<T> destination)
        where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = TElementMarshaller.ConvertToManaged(source[i]);
        }
    }
}
#endif