// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of blittable Windows Runtime types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class WindowsRuntimeBlittableValueTypeArrayMarshaller
{
    /// <summary>
    /// Clears the unmanaged storage for the specified array without disposing its elements.
    /// </summary>
    /// <param name="size">The number of elements in the array.</param>
    /// <param name="array">The input array, or <see langword="null"/> for no storage.</param>
    /// <param name="elementSize">The size of each ABI element, in bytes.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Clear(uint size, void* array, int elementSize)
    {
        if (array is null)
        {
            return;
        }

        NativeMemory.Clear(array, checked(size * (nuint)elementSize));
    }

    /// <summary>
    /// Frees the specified array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    public static void Free(uint size, void* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        Marshal.FreeCoTaskMem((nint)array);
    }
}
