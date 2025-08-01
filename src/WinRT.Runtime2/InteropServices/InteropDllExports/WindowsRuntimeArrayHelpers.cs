// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A helper for generated marshaller types for Windows Runtime arrays.
/// </summary>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeArrayHelpers
{
    /// <summary>
    /// Frees an <c>HSTRING</c> reference array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeHStringArrayUnsafe(uint size, HSTRING* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < size; i++)
        {
            HStringMarshaller.Free(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }

    /// <summary>
    /// Frees an object reference array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeObjectArrayUnsafe(uint size, void** array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < size; i++)
        {
            WindowsRuntimeObjectMarshaller.Free(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }

    /// <summary>
    /// Frees a reference array of some blittable type.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeBlittableArrayUnsafe(uint size, void** array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        Marshal.FreeCoTaskMem((nint)array);
    }

    /// <summary>
    /// Frees a range of an <c>HSTRING</c> reference array.
    /// </summary>
    /// <param name="offset">The offset to free the array up to.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeHStringRangeUnsafe(int offset, HSTRING* array)
    {
        for (int i = 0; i < offset; i++)
        {
            HStringMarshaller.Free(array[i]);
        }
    }

    /// <summary>
    /// Frees a range of an object reference array.
    /// </summary>
    /// <param name="offset">The offset to free the array up to.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeObjectRangeUnsafe(int offset, void** array)
    {
        for (int i = 0; i < offset; i++)
        {
            WindowsRuntimeObjectMarshaller.Free(array[i]);
        }
    }
}
