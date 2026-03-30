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
    /// A <see cref="WindowsRuntimeNativeStreamAdapter"/> implementation for <see cref="IInputStream"/>.
    /// </summary>
    private sealed class InputStream : WindowsRuntimeNativeStreamAdapter, IInputStream
    {
        /// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter.WindowsRuntimeNativeStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="InputStream"/> instance with the specified parameters.
        /// </summary>
        public InputStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}
#endif