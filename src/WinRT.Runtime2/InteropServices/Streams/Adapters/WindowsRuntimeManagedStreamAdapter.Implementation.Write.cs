// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Tasks;
using Windows.Storage.Buffers;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeManagedStreamAdapter"/>
internal partial class WindowsRuntimeManagedStreamAdapter
{
    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return BeginWrite(buffer, offset, count, callback, state, usedByBlockingWrapper: false);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    private StreamWriteAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state, bool usedByBlockingWrapper)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfInsufficientArrayElementsAfterOffset(buffer.Length, offset, count);

        IOutputStream windowsRuntimeStream = (IOutputStream)EnsureNotDisposed();

        NotSupportedException.ThrowIfStreamCannotWrite(_canWrite);

        IBuffer asyncWriteBuffer = buffer.AsBuffer(offset, count);

        // See the large comment in the 'BeginRead' method about why we are not using the
        // 'WriteAsync' method, and instead using a custom implementation of 'IAsyncResult'.
        IAsyncOperationWithProgress<uint, uint> asyncWriteOperation = windowsRuntimeStream.WriteAsync(asyncWriteBuffer);

        // See additional notes in the 'Read' method about how CCW objects for this result are managed
        return new StreamWriteAsyncResult(
            asyncWriteOperation,
            callback,
            state,
            processCompletedOperationInCallback: !usedByBlockingWrapper);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override void EndWrite(IAsyncResult asyncResult)
    {
        ArgumentNullException.ThrowIfNull(asyncResult);
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotWrite(_canWrite);

        // We can only perform this operation if we have our own async result instance
        if (asyncResult is not StreamOperationAsyncResult streamAsyncResult)
        {
            throw ArgumentException.GetUnexpectedAsyncResultException(nameof(asyncResult));
        }

        streamAsyncResult.Wait();

        try
        {
            // Process the completed operation if needed (see additional notes in 'EndRead')
            if (!streamAsyncResult.ProcessCompletedOperationInCallback)
            {
                streamAsyncResult.ProcessCompletedOperation();
            }

            // Rethrow any errors caught in the completion callback
            if (streamAsyncResult.HasError)
            {
                streamAsyncResult.CloseStreamOperation();
                streamAsyncResult.ThrowCachedError();
            }
        }
        finally
        {
            // Closing an operation multiple times is fine
            streamAsyncResult.CloseStreamOperation();
        }
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfInsufficientArrayElementsAfterOffset(buffer.Length, offset, count);

        IOutputStream windowsRuntimeStream = (IOutputStream)EnsureNotDisposed();

        NotSupportedException.ThrowIfStreamCannotWrite(_canWrite);

        // If already cancelled, stop early
        cancellationToken.ThrowIfCancellationRequested();

        IBuffer asyncWriteBuffer = buffer.AsBuffer(offset, count);

        // The underlying 'IBuffer' object is the only object to which we expose a direct pointer
        // to native, and that is properly pinned using a mechanism similar to 'Overlapped'.
        return windowsRuntimeStream.WriteAsync(asyncWriteBuffer).AsTask(cancellationToken);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override void Write(byte[] buffer, int offset, int count)
    {
        // Arguments validation and disposal validation are done in 'BeginWrite'

        StreamWriteAsyncResult asyncResult = BeginWrite(buffer, offset, count, null, null, usedByBlockingWrapper: true);

        EndWrite(asyncResult);
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        // We don't need to call 'EnsureNotDisposed', see notes in 'ReadByte'
        Write(new ReadOnlySpan<byte>(in value));
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override void Flush()
    {
        IOutputStream windowsRuntimeStream = (IOutputStream)EnsureNotDisposed();

        // Calling 'Flush' in a non-writeable stream is a no-op, not an error
        if (!_canWrite)
        {
            return;
        }

        IAsyncOperation<bool> asyncFlushOperation = windowsRuntimeStream.FlushAsync();

        // See the large comment in 'BeginRead' about why we are not using 'FlushAsync', and instead
        // using a custom implementation of 'IAsyncResult' (we do the same for reads and writes too).
        StreamFlushAsyncResult asyncResult = new(asyncFlushOperation);

        asyncResult.Wait();

        try
        {
            // We got signaled, so process the 'Flush' operation back on this thread. This
            // is to allow blocking-over-async I/O operations (see notes in 'BeginRead').
            asyncResult.ProcessCompletedOperation();

            // Rethrow errors cached by the async result, if any
            if (asyncResult.HasError)
            {
                asyncResult.CloseStreamOperation();
                asyncResult.ThrowCachedError();
            }
        }
        finally
        {
            // Closing an operation multiple times is fine
            asyncResult.CloseStreamOperation();
        }
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        IOutputStream windowsRuntimeStream = (IOutputStream)EnsureNotDisposed();

        // Calling Flush in a non-writeable stream is a no-op, not an error
        if (!_canWrite)
        {
            return Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return windowsRuntimeStream.FlushAsync().AsTask(cancellationToken);
    }
}