// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of blittable Windows Runtime types.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public static unsafe class WindowsRuntimeBlittableValueTypeArrayMarshaller<T>
    where T : unmanaged
{
    /// <summary>
    /// Marshals a managed array to an unmanaged Windows Runtime array.
    /// </summary>
    /// <param name="source">The source array.</param>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The resulting Windows Runtime array.</param>
    public static void ConvertToUnmanaged(ReadOnlySpan<T> source, out uint size, out T* array)
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        size = (uint)source.Length;
        array = (T*)Marshal.AllocCoTaskMem(sizeof(T) * source.Length);

        source.CopyTo(new Span<T>(array, source.Length));
    }

    /// <summary>
    /// Marshals an unmanaged Windows Runtime array to a managed array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="value">The source array.</param>
    /// <returns>The resulting managed array.</returns>
    public static T[] ConvertToManaged(uint size, T* value)
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        return new ReadOnlySpan<T>(value, (int)size).ToArray();
    }

    /// <summary>
    /// Copies items from a managed array to the target unmanaged Windows Runtime array.
    /// </summary>
    /// <param name="source">The source array.</param>
    /// <param name="size">The size of the array.</param>
    /// <param name="destination">The destination array.</param>
    public static void CopyToUnmanaged(ReadOnlySpan<T> source, uint size, T* destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(source.Length, size);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(destination);

        source.CopyTo(new Span<T>(destination, (int)size));
    }

    /// <summary>
    /// Copies items from an unmanaged Windows Runtime array to the target managed array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="source">The source array.</param>
    /// <param name="destination">The destination array.</param>
    public static void CopyToManaged(uint size, T* source, Span<T> destination)
    {
        WindowsRuntimeArrayMarshallerHelpers.ValidateDestinationSize(size, destination.Length);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        new ReadOnlySpan<T>(source, (int)size).CopyTo(destination);
    }
}
#endif
