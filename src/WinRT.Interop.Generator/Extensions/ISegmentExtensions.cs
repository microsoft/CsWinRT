// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
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
    public static unsafe bool TryWriteExactly(this ISegment segment, Span<byte> span)
    {
        // If the segment is readable, we can just read from it directly, which is more efficient
        if (segment is IReadableSegment readableSegment)
        {
            int bytesWritten = readableSegment.CreateReader().ReadBytes(span);

            return bytesWritten == span.Length;
        }

        // Otherwise, write the segment data to the target span via a wrapper stream and writer
        fixed (byte* spanPtr = span)
        {
            using UnmanagedMemoryStream stream = new(
                pointer: spanPtr,
                length: span.Length,
                capacity: span.Length,
                access: FileAccess.Write);

            segment.Write(new BinaryStreamWriter(stream));

            return stream.Position == span.Length;
        }
    }
}