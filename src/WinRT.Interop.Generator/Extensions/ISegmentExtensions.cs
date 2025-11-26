// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.IO;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="ISegment"/>.
/// </summary>
internal static class ISegmentExtensions
{
    /// <summary>
    /// Attempts to read exactly enough data from a given segment into a target span.
    /// </summary>
    /// <param name="segment">The segment to read data from.</param>
    /// <param name="span">The destination span to write data to.</param>
    /// <returns>Whether enough data from <paramref name="segment"/> was written into <paramref name="span"/>.</returns>
    public static bool TryReadExactly(this ISegment segment, Span<byte> span)
    {
        // Validate that the segment has exactly enough data to fill the target span
        if (segment.GetPhysicalSize() != (ulong)span.Length)
        {
            return false;
        }

        // If the segment is readable, we can just read from it directly, which is more efficient.
        // This is actually guaranteed to be the case for RVA segments of .dll-s we read from disk.
        if (segment is IReadableSegment readableSegment)
        {
            int bytesWritten = readableSegment.CreateReader().ReadBytes(span);

            return bytesWritten == span.Length;
        }

        // Less efficient fallback path (this should never be hit)
        byte[] data = segment.WriteIntoArray();

        // Validate again that the effective length matches, same as above
        if (data.Length == span.Length)
        {
            data.CopyTo(span);

            return true;
        }

        return false;
    }
}