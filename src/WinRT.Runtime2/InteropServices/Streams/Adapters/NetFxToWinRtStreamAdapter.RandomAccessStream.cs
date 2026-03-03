// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="NetFxToWinRtStreamAdapter"/>
internal partial class NetFxToWinRtStreamAdapter
{
    /// <summary>
    /// A <see cref="NetFxToWinRtStreamAdapter"/> implementation for <see cref="IRandomAccessStream"/>.
    /// </summary>
    private sealed class RandomAccessStream : NetFxToWinRtStreamAdapter, IRandomAccessStream
    {
        /// <inheritdoc cref="NetFxToWinRtStreamAdapter.NetFxToWinRtStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="RandomAccessStream"/> instance with the specified parameters.
        /// </summary>
        public RandomAccessStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}