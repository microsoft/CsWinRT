// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="NetFxToWinRtStreamAdapter"/>
internal partial class NetFxToWinRtStreamAdapter
{
    /// <summary>
    /// A <see cref="NetFxToWinRtStreamAdapter"/> implementation for <see cref="IOutputStream"/>.
    /// </summary>
    private sealed class OutputStream : NetFxToWinRtStreamAdapter, IOutputStream
    {
        /// <inheritdoc cref="NetFxToWinRtStreamAdapter.NetFxToWinRtStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="OutputStream"/> instance with the specified parameters.
        /// </summary>
        public OutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}
