// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of <see cref="Nullable{T}"/> types.
/// </summary>
/// <typeparam name="T">The underlying value type of the nullable type.</typeparam>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class WindowsRuntimeNullableTypeArrayMarshaller<T>
    where T : struct
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void ConvertToUnmanaged<TElementMarshaller>(ReadOnlySpan<T?> source, out uint size, out void** array)
        where TElementMarshaller : IWindowsRuntimeNullableTypeArrayElementMarshaller<T>
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        void** destination = (void**)Marshal.AllocCoTaskMem(sizeof(void*) * source.Length);

        int i = 0;

        try
        {
            // Marshal all array elements (the ABI type for 'Nullable<T>' is just 'void*')
            for (; i < source.Length; i++)
            {
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]).DetachThisPtrUnsafe();
            }
        }
        catch
        {
            // Release any allocated native 'IReference<T>' values
            for (int j = 0; j < i; j++)
            {
                WindowsRuntimeUnknownMarshaller.Free(destination[j]);
            }

            // Also release the allocated array to avoid leaking
            Marshal.FreeCoTaskMem((nint)destination);

            throw;
        }

        size = (uint)source.Length;
        array = destination;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static T?[] ConvertToManaged<TElementMarshaller>(uint size, void** value)
        where TElementMarshaller : IWindowsRuntimeNullableTypeArrayElementMarshaller<T>
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        T?[] array = new T?[(int)size];

        for (int i = 0; i < size; i++)
        {
            array[i] = TElementMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToUnmanaged<TElementMarshaller>(ReadOnlySpan<T?> source, uint size, void** destination)
        where TElementMarshaller : IWindowsRuntimeNullableTypeArrayElementMarshaller<T>
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(source.Length, size);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(destination);

        int i = 0;

        try
        {
            // Marshal the items in the input span
            for (; i < source.Length; i++)
            {
                destination[i] = TElementMarshaller.ConvertToUnmanaged(source[i]).DetachThisPtrUnsafe();
            }
        }
        catch
        {
            // Release resources for any items, if we failed
            for (int j = 0; j < i; j++)
            {
                WindowsRuntimeUnknownMarshaller.Free(destination[j]);
            }

            throw;
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    /// <typeparam name="TElementMarshaller">The type of marshaller for each managed array element.</typeparam>
    public static void CopyToManaged<TElementMarshaller>(uint size, void** source, Span<T?> destination)
        where TElementMarshaller : IWindowsRuntimeNullableTypeArrayElementMarshaller<T>
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
