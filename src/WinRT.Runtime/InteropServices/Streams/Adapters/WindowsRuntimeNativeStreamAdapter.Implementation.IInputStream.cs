// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter"/>
internal partial class WindowsRuntimeNativeStreamAdapter
{
    /// <inheritdoc cref="IInputStream.ReadAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfCountExceedsInt32MaxValue(count);
        ArgumentException.ThrowIfBufferCapacityInsufficient(buffer.Capacity, count);
        ArgumentOutOfRangeException.ThrowIfInvalidInputStreamOptions(options);

        Stream managedStream = EnsureNotDisposed();

        // We can use this pattern to add more optimization options if necessary:
        //
        // StreamReadOperationOptimization.XxxxStream => StreamOperationsImplementation.Xxxx,ReadAsync(managedStream, buffer, count, options);
        return _readOptimization switch
        {
            StreamReadOperationOptimization.MemoryStream => StreamOperationsImplementation.MemoryStream.ReadAsync(managedStream, buffer, count),
            StreamReadOperationOptimization.AbstractStream => StreamOperationsImplementation.ReadAsync(managedStream, buffer, count, options),
            _ => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_UnrecognizedStreamReadOptimization)
        };
    }
}