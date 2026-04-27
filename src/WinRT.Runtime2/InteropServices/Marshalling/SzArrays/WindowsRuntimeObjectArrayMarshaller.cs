// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of Windows Runtime objects.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class WindowsRuntimeObjectArrayMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    public static void ConvertToUnmanaged(ReadOnlySpan<object?> source, out uint size, out void** array)
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
            // Marshal all array elements as 'IInspectable' objects
            for (; i < source.Length; i++)
            {
                destination[i] = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(source[i]).DetachThisPtrUnsafe();
            }
        }
        catch
        {
            // Make sure to release all marshalled objects so far (this shouldn't ever throw)
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
    public static object?[] ConvertToManaged(uint size, void** value)
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        object?[] array = new object[(int)size];

        for (int i = 0; i < size; i++)
        {
            array[i] = WindowsRuntimeObjectMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    public static void CopyToUnmanaged(ReadOnlySpan<object?> source, uint size, void** destination)
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
                destination[i] = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(source[i]).DetachThisPtrUnsafe();
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
    public static void CopyToManaged(uint size, void** source, Span<object?> destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = WindowsRuntimeObjectMarshaller.ConvertToManaged(source[i]);
        }
    }
}
