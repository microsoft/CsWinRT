// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.IO;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter"/>
internal partial class WindowsRuntimeNativeStreamAdapter
{
    /// <inheritdoc cref="IOutputStream.WriteAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentException.ThrowIfBufferLengthExceedsBufferCapacity(buffer.Capacity, buffer.Length);

        Stream managedStream = EnsureNotDisposed();

        return StreamOperationsImplementation.WriteAsync(managedStream, buffer);
    }

    /// <inheritdoc cref="IOutputStream.FlushAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperation<bool> FlushAsync()
    {
        Stream managedStream = EnsureNotDisposed();

        return StreamOperationsImplementation.FlushAsync(managedStream);
    }
}
#endif