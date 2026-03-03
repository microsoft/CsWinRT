// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="NetFxToWinRtStreamAdapter"/>
internal partial class NetFxToWinRtStreamAdapter
{
    /// <summary>
    /// A <see cref="NetFxToWinRtStreamAdapter"/> implementation for <see cref="IInputStream"/>.
    /// </summary>
    private sealed class InputStream : NetFxToWinRtStreamAdapter, IInputStream
    {
        /// <inheritdoc cref="NetFxToWinRtStreamAdapter.NetFxToWinRtStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="InputStream"/> instance with the specified parameters.
        /// </summary>
        public InputStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}