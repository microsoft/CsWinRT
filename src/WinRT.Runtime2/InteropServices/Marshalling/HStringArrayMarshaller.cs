// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for arrays of the Windows Runtime <c>HSTRING</c> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class HStringArrayMarshaller
{
    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> value to an unmanaged <c>HSTRING</c>.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> value to convert.</param>
    /// <returns>The resulting <c>HSTRING</c>.</returns>
    /// <remarks>It is responsibility of callers to free the returned <c>HSTRING</c> value.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ConvertToUnmanaged(ReadOnlySpan<string> source, out uint size, out HSTRING* array)
    {
        if (source.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        size = (uint)source.Length;
        array = (HSTRING*)Marshal.AllocCoTaskMem(sizeof(HSTRING) * source.Length);

        int i = 0;

        try
        {
            for (i = 0; i < source.Length; i++)
            {
                array[i] = HStringMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            Free(size: (uint)i, array);

            size = 0;
            array = null;

            throw;
        }

    }

    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> value to an unmanaged <c>HSTRING</c>.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> value to convert.</param>
    /// <returns>The resulting <c>HSTRING</c>.</returns>
    /// <remarks>It is responsibility of callers to free the returned <c>HSTRING</c> value.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CopyToUnmanaged(ReadOnlySpan<string> source, uint size, HSTRING* array)
    {
        WindowsRuntimeArrayHelpers.ValidateDestinationSize(source, size);

        if (source.IsEmpty)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        int i = 0;

        try
        {
            // Marshal all items, and keep track of how many have been marshaled in case of an exception
            for (i = 0; i < source.Length; i++)
            {
                array[i] = HStringMarshaller.ConvertToUnmanaged(source[i]);
            }
        }
        catch
        {
            FreeRange(size: (uint)i, array);

            throw;
        }
    }

    /// <summary>
    /// Frees a range of an <c>HSTRING</c> reference array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeRange(uint size, HSTRING* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            HStringMarshaller.Free(array[i]);

            array[i] = null;
        }
    }

    /// <summary>
    /// Frees an <c>HSTRING</c> reference array.
    /// </summary>
    /// <param name="size">The size of the array.</param>
    /// <param name="array">The input array.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Free(uint size, HSTRING* array)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(array);

        for (uint i = 0; i < size; i++)
        {
            HStringMarshaller.Free(array[i]);
        }

        Marshal.FreeCoTaskMem((nint)array);
    }
}