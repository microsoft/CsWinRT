// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A helper for generated marshaller types for Windows Runtime arrays.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeArrayHelpers
{
    /// <summary>
    /// Validates that the specified destination span has the required number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the destination span.</typeparam>
    /// <param name="size">The expected number of elements in the destination span.</param>
    /// <param name="destination">The span to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the length of <paramref name="destination"/> does not equal <paramref name="size"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [StackTraceHidden]
    public static void ValidateDestinationSize<T>(uint size, Span<T> destination)
    {
        if (destination.Length != size)
        {
            [StackTraceHidden]
            static void ThrowArgumentException() => throw new ArgumentException("The destination array is too small.", nameof(destination));

            ThrowArgumentException();
        }
    }

    /// <summary>
    /// Validates that the specified destination span has the required number of elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the destination span.</typeparam>
    /// <param name="source">The span to validate.</param>
    /// <param name="size">The expected number of elements in the destination span.</param>
    /// <exception cref="ArgumentException">Thrown if the length of <paramref name="source"/> does not equal <paramref name="size"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [StackTraceHidden]
    public static void ValidateDestinationSize<T>(ReadOnlySpan<T> source, uint size)
    {
        if (source.Length != size)
        {
            [StackTraceHidden]
            static void ThrowArgumentException() => throw new ArgumentException("The destination array is too small.", "destination");

            ThrowArgumentException();
        }
    }

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
    /// Frees a reference array of <see cref="Type"/> values.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeTypeArrayUnsafe(uint size, ABI.System.Type* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < size; i++)
        {
            HStringMarshaller.Free(array[i].Name);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }

    /// <summary>
    /// Frees a reference array of some blittable type.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeBlittableArrayUnsafe(uint size, void* array)
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
