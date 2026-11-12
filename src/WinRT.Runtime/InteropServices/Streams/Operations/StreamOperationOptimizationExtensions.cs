// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides helpers to determine various kinds of stream optimizations.
/// </summary>
internal static class StreamOperationOptimizationExtensions
{
    extension(StreamReadOperationOptimization)
    {
        /// <summary>
        /// Gets the <see cref="StreamReadOperationOptimization"/> value for a given <see cref="Stream"/> object.
        /// </summary>
        /// <param name="stream">The input <see cref="Stream"/> object to check.</param>
        /// <returns>The resulting <see cref="StreamReadOperationOptimization"/> value for <paramref name="stream"/>.</returns>
        public static StreamReadOperationOptimization Determine(Stream stream)
        {
            // Helper to check if the input stream is an unwrappable 'MemoryStream'
            static bool CanUseMemoryStreamOptimization(Stream stream)
            {
                // If the input stream is not some 'MemoryStream' instance, we can't optimize for it
                if (stream is not MemoryStream memoryStream)
                {
                    return false;
                }

                // This specific optimization relies on being able to unwrap the underlying buffer of
                // the stream. We don't need to store it, the assumption is that if we can retrieve
                // the buffer here, it means we'll always be able to successfully retrieve it later.
                return memoryStream.TryGetBuffer(out _);
            }

            // Check all available optimizations in priority order, starting with 'MemoryStream'
            if (CanUseMemoryStreamOptimization(stream))
            {
                return StreamReadOperationOptimization.MemoryStream;
            }

            // If all optimizations failed, then we just use the default one for all stream types
            return StreamReadOperationOptimization.AbstractStream;
        }
    }
}
