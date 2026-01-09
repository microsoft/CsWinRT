// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of blittable Windows Runtime types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class BlittableValueTypeArrayMarshaller
{
    /// <summary>
    /// Marshals a managed array to an unmanaged Windows Runtime array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="source">The source array.</param>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The resulting Windows Runtime array.</param>
    public static void ConvertToUnmanaged<T>(ReadOnlySpan<T> source, out uint size, out T* array)
        where T : unmanaged
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
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="size">The size of the array.</param>
    /// <param name="value">The source array.</param>
    /// <returns>The resulting managed array.</returns>
    public static T[] ConvertToManaged<T>(uint size, T* value)
        where T : unmanaged
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
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="source">The source array.</param>
    /// <param name="size">The size of the array.</param>
    /// <param name="destination">The destination array.</param>
    public static void CopyToUnmanaged<T>(ReadOnlySpan<T> source, uint size, T* destination)
        where T : unmanaged
    {
        WindowsRuntimeArrayHelpers.ValidateDestinationSize(source, size);

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
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="size">The size of the array.</param>
    /// <param name="source">The source array.</param>
    /// <param name="destination">The destination array.</param>
    public static void CopyToManaged<T>(uint size, T* source, Span<T> destination)
        where T : unmanaged
    {
        WindowsRuntimeArrayHelpers.ValidateDestinationSize(size, destination);

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(source);

        new ReadOnlySpan<T>(source, (int)size).CopyTo(destination);
    }

    /// <summary>
    /// Frees the specified array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
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