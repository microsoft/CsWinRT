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
    /// <inheritdoc cref="IInputStream.ReadAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (count is < 0 or > int.MaxValue)
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(count));
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        if (buffer.Capacity < count)
        {
            ArgumentException ex = new ArgumentException(SR.Argument_InsufficientBufferCapacity);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        if (!(options == InputStreamOptions.None || options == InputStreamOptions.Partial || options == InputStreamOptions.ReadAhead))
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        Stream managedStream = EnsureNotDisposed();

        // We can use this pattern to add more optimization options if necessary:
        //
        // StreamReadOperationOptimization.XxxxStream => StreamOperationsImplementation.Xxxx,ReadAsync(managedStream, buffer, count, options);
        return _readOptimization switch
        {
            StreamReadOperationOptimization.MemoryStream => StreamOperationsImplementation.MemoryStream.ReadAsync(managedStream, buffer, count),
            StreamReadOperationOptimization.AbstractStream => StreamOperationsImplementation.ReadAsync(managedStream, buffer, count, options),
            _ => throw new NotSupportedException(SR.NotSupported_UnrecognizedStreamReadOptimization)
        };
    }
}