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
