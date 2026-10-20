// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for span types.
/// </summary>
internal static class MemoryExtensions
{
    extension(ReadOnlySpan<string> left)
    {
        /// <summary>
        /// Compares two string sequences lexicographically using ordinal string comparison.
        /// </summary>
        /// <param name="right">The sequence to compare with.</param>
        /// <returns>A negative value, zero, or a positive value if the left sequence is less than, equal to, or greater than the right sequence.</returns>
        public int CompareLexicographically(ReadOnlySpan<string> right)
        {
            for (int i = 0; i < Math.Min(left.Length, right.Length); i++)
            {
                int result = StringComparer.Ordinal.Compare(left[i], right[i]);

                if (result != 0)
                {
                    return result;
                }
            }

            return left.Length.CompareTo(right.Length);
        }
    }

    extension<T>(ReadOnlySpan<T> span)
    {
        /// <summary>
        /// Converts a <see cref="ReadOnlySpan{T}"/> value to a <see cref="Span{T}"/> value.
        /// </summary>
        /// <returns>The resulting <see cref="Span{T}"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpanUnsafe()
        {
            return MemoryMarshal.CreateSpan(
                reference: ref MemoryMarshal.GetReference(span),
                length: span.Length);
        }
    }
}