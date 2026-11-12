// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter"/>
internal partial class WindowsRuntimeNativeStreamAdapter
{
    /// <summary>
    /// A <see cref="WindowsRuntimeNativeStreamAdapter"/> implementation for <see cref="IInputStream"/> and <see cref="IOutputStream"/>.
    /// </summary>
    private sealed class InputOutputStream : WindowsRuntimeNativeStreamAdapter, IInputStream, IOutputStream
    {
        /// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter.WindowsRuntimeNativeStreamAdapter"/>
        /// <summary>
        /// Creates a new <see cref="InputOutputStream"/> instance with the specified parameters.
        /// </summary>
        public InputOutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
            : base(stream, readOptimization)
        {
        }
    }
}