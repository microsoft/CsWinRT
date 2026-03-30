// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter"/>
internal partial class WindowsRuntimeNativeStreamAdapter
{
    /// <summary>
    /// A <see cref="WindowsRuntimeNativeStreamAdapter"/> implementation for <see cref="IRandomAccessStream"/>.
    /// </summary>
    private sealed class RandomAccessStream : WindowsRuntimeNativeStreamAdapter, IRandomAccessStream
    {
        /// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter.WindowsRuntimeNativeStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="RandomAccessStream"/> instance with the specified parameters.
        /// </summary>
        public RandomAccessStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}
#endif