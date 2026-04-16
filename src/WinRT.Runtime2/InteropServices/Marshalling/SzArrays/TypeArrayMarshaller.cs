// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of the Windows Runtime <see cref="Type"/> type.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class TypeArrayMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToUnmanaged"/>
    public static void ConvertToUnmanaged(ReadOnlySpan<Type> source, out uint size, out ABI.System.Type* array)
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        ABI.System.Type* destination = (ABI.System.Type*)Marshal.AllocCoTaskMem(sizeof(ABI.System.Type) * source.Length);

        int i = 0;

        try
        {
            // Marshal all input 'Type' objects with its marshaller (note the ABI type will be a value type)
            for (; i < source.Length; i++)
            {
                destination[i] = ABI.System.TypeMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Dispose each ABI value (it contains an 'HSTRING' which we should release)
            for (int j = 0; j < i; j++)
            {
                ABI.System.TypeMarshaller.Dispose(destination[j]);
            }

            // Also release the allocated array to avoid leaking
            Marshal.FreeCoTaskMem((nint)destination);

            throw;
        }

        size = (uint)source.Length;
        array = destination;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.ConvertToManaged"/>
    public static Type?[] ConvertToManaged(uint size, ABI.System.Type* value)
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        Type?[] array = new Type[(int)size];

        for (int i = 0; i < size; i++)
        {
            array[i] = ABI.System.TypeMarshaller.ConvertToManaged(value[i]);
        }

        return array;
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToUnmanaged"/>
    public static void CopyToUnmanaged(ReadOnlySpan<Type> source, uint size, ABI.System.Type* destination)
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
                destination[i] = ABI.System.TypeMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            // Release resources for any items, if we failed
            for (int j = 0; j < i; j++)
            {
                ABI.System.TypeMarshaller.Dispose(destination[j]);
            }

            throw;
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller{T}.CopyToManaged"/>
    public static void CopyToManaged(uint size, ABI.System.Type* source, Span<Type?> destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = ABI.System.TypeMarshaller.ConvertToManaged(source[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeUnknownArrayMarshaller.Dispose"/>
    public static void Dispose(uint size, ABI.System.Type* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            ABI.System.TypeMarshaller.Dispose(array[i]);
        }
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableValueTypeArrayMarshaller.Free"/>
    public static void Free(uint size, ABI.System.Type* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            ABI.System.TypeMarshaller.Dispose(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }
}
