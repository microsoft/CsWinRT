// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Tasks;
using Windows.Storage.Buffers;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WinRtToNetFxStreamAdapter"/>
internal partial class WinRtToNetFxStreamAdapter
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
        // See the large comment in BeginRead about why we are not using this.WriteAsync,
        // and instead using a custom implementation of IAsyncResult.

        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfInsufficientArrayElementsAfterOffset(buffer.Length, offset, count);

        IOutputStream wrtStr = (IOutputStream)EnsureNotDisposed();
        NotSupportedException.ThrowIfStreamCannotWrite(_canWrite);

        Debug.Assert(wrtStr != null);

        IBuffer asyncWriteBuffer = buffer.AsBuffer(offset, count);

        IAsyncOperationWithProgress<uint, uint> asyncWriteOperation = wrtStr.WriteAsync(asyncWriteBuffer);

        StreamWriteAsyncResult asyncResult = new(asyncWriteOperation, callback, state,
                                                                        processCompletedOperationInCallback: !usedByBlockingWrapper);

        // The StreamReadAsyncResult will set a private instance method to act as a Completed handler for asyncOperation.
        // This will cause a CCW to be created for the delegate and the delegate has a reference to its target, i.e. to
        // asyncResult, so asyncResult will not be collected. If we loose the entire AppDomain, then asyncResult and its CCW
        // will be collected but the stub will remain and the callback will fail gracefully. The underlying buffer if the only
        // item to which we expose a direct pointer and this is properly pinned using a mechanism similar to Overlapped.

        return asyncResult;
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
            // If the async result did NOT process the async IO operation in its completion handler (i.e. check for errors,
            // cache results etc), then we need to do that processing now. This is to allow blocking-over-async IO operations.
            // See the big comment in BeginWrite for details.

            if (!streamAsyncResult.ProcessCompletedOperationInCallback)
            {
                streamAsyncResult.ProcessCompletedOperation();
            }

            // Rethrow errors caught in the completion callback, if any:
            if (streamAsyncResult.HasError)
            {
                streamAsyncResult.CloseStreamOperation();
                streamAsyncResult.ThrowCachedError();
            }
        }
        finally
        {
            // Closing multiple times is Ok.
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

        IOutputStream wrtStr = (IOutputStream)EnsureNotDisposed();
        NotSupportedException.ThrowIfStreamCannotWrite(_canWrite);

        // If already cancelled, bail early:
        cancellationToken.ThrowIfCancellationRequested();

        IBuffer asyncWriteBuffer = buffer.AsBuffer(offset, count);

        IAsyncOperationWithProgress<uint, uint> asyncWriteOperation = wrtStr.WriteAsync(asyncWriteBuffer);
        Task asyncWriteTask = asyncWriteOperation.AsTask(cancellationToken);

        // The underlying IBuffer is the only object to which we expose a direct pointer to native,
        // and that is properly pinned using a mechanism similar to Overlapped.

        return asyncWriteTask;
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override void Write(byte[] buffer, int offset, int count)
    {
        // Arguments validation and not-disposed validation are done in BeginWrite.

        IAsyncResult asyncResult = BeginWrite(buffer, offset, count, null, null, usedByBlockingWrapper: true);
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
        // See the large comment in BeginRead about why we are not using this.FlushAsync,
        // and instead using a custom implementation of IAsyncResult.

        IOutputStream wrtStr = (IOutputStream)EnsureNotDisposed();

        // Calling Flush in a non-writable stream is a no-op, not an error:
        if (!_canWrite)
        {
            return;
        }

        IAsyncOperation<bool> asyncFlushOperation = wrtStr.FlushAsync();
        StreamFlushAsyncResult asyncResult = new(asyncFlushOperation);

        asyncResult.Wait();

        try
        {
            // We got signaled, so process the async Flush operation back on this thread:
            // (This is to allow blocking-over-async IO operations. See the big comment in BeginRead for details.)
            asyncResult.ProcessCompletedOperation();

            // Rethrow errors cached by the async result, if any:
            if (asyncResult.HasError)
            {
                asyncResult.CloseStreamOperation();
                asyncResult.ThrowCachedError();
            }
        }
        finally
        {
            // Closing multiple times is Ok.
            asyncResult.CloseStreamOperation();
        }
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        IOutputStream wrtStr = (IOutputStream)EnsureNotDisposed();

        // Calling Flush in a non-writable stream is a no-op, not an error:
        if (!_canWrite)
        {
            return Task.CompletedTask;
        }

        cancellationToken.ThrowIfCancellationRequested();

        IAsyncOperation<bool> asyncFlushOperation = wrtStr.FlushAsync();
        Task asyncFlushTask = asyncFlushOperation.AsTask(cancellationToken);
        return asyncFlushTask;
    }
}