// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="NetFxToWinRtStreamAdapter"/>
internal partial class NetFxToWinRtStreamAdapter
{
    /// <summary>
    /// A <see cref="NetFxToWinRtStreamAdapter"/> implementation for <see cref="IInputStream"/> and <see cref="IOutputStream"/>.
    /// </summary>
    private sealed class InputOutputStream : NetFxToWinRtStreamAdapter, IInputStream, IOutputStream
    {
        /// <inheritdoc cref="NetFxToWinRtStreamAdapter.NetFxToWinRtStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="InputOutputStream"/> instance with the specified parameters.
        /// </summary>
        public InputOutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}