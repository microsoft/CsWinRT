// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Helpers for adapter methods for <see cref="IReadOnlyList{T}"/>.
/// </summary>
internal static class IReadOnlyListAdapterHelpers
{
    /// <summary>
    /// Ensures that the specified index is within the valid range for a collection of the specified count.
    /// </summary>
    /// <param name="index">The input index.</param>
    /// <param name="count">The size of the collection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureIndexInValidRange(uint index, int count)
    {
        if (index >= (uint)count)
        {
            [DoesNotReturn]
            static void ThrowArgumentOutOfRangeException()
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_IndexLargerThanMaxValue")
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };
            }

            ThrowArgumentOutOfRangeException();
        }
    }
}