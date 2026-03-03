// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="NetFxToWinRtStreamAdapter"/>
internal partial class NetFxToWinRtStreamAdapter
{
    /// <inheritdoc cref="IOutputStream.WriteAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Capacity < buffer.Length)
        {
            ArgumentException ex = new ArgumentException(SR.Argument_BufferLengthExceedsCapacity);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

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