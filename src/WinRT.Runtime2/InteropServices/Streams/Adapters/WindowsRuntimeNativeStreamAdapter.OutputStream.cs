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
    /// A <see cref="WindowsRuntimeNativeStreamAdapter"/> implementation for <see cref="IOutputStream"/>.
    /// </summary>
    private sealed class OutputStream : WindowsRuntimeNativeStreamAdapter, IOutputStream
    {
        /// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter.WindowsRuntimeNativeStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="OutputStream"/> instance with the specified parameters.
        /// </summary>
        public OutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}
#endif
