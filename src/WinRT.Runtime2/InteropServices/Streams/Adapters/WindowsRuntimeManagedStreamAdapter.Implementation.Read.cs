// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeManagedStreamAdapter"/>
internal partial class WindowsRuntimeManagedStreamAdapter
{
    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override int EndRead(IAsyncResult asyncResult)
    {
        ArgumentNullException.ThrowIfNull(asyncResult);

        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        // We can only perform this operation if we have our own async result instance
        if (asyncResult is not StreamOperationAsyncResult streamAsyncResult)
        {
            throw ArgumentException.GetUnexpectedAsyncResultException(nameof(asyncResult));
        }

        streamAsyncResult.Wait();

        try
        {
            // If the async result did not process the async I/O operation in its completion handler
            // (i.e. check for errors, cache results etc), then we need to do that processing now.
            // This is to allow blocking-over-async I/O operations. See additional notes in the
            // 'BeginRead' method below for more details.
            if (!streamAsyncResult.ProcessCompletedOperationInCallback)
            {
                streamAsyncResult.ProcessCompletedOperation();
            }

            // Rethrow errors caught in the completion callback, if any
            if (streamAsyncResult.HasError)
            {
                streamAsyncResult.CloseStreamOperation();
                streamAsyncResult.ThrowCachedError();
            }

            long bytesCompleted = streamAsyncResult.NumberOfBytesProcessed;

            Debug.Assert(bytesCompleted <= unchecked(int.MaxValue));

            return (int)bytesCompleted;
        }
        finally
        {
            // Closing an operation multiple times is fine
            streamAsyncResult.CloseStreamOperation();
        }
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfInsufficientSpaceInTargetBuffer(buffer.Length, offset, count);
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        // If already cancelled, stop early
        cancellationToken.ThrowIfCancellationRequested();

        if (count == 0)
        {
            return Task.FromResult(0);
        }

        // Helper to perform the actual asynchronous read operation
        async Task<int> ReadCoreAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            IInputStream windowsRuntimeStream = (IInputStream)EnsureNotDisposed();

            try
            {
                IBuffer userBuffer = buffer.AsBuffer(offset, count);

                IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = windowsRuntimeStream.ReadAsync(
                    buffer: userBuffer,
                    count: unchecked((uint)count),
                    options: InputStreamOptions.Partial);

                IBuffer? resultBuffer = await asyncReadOperation.AsTask(cancellationToken).ConfigureAwait(false);

                // If the input cancellation token was cancelled until now, then we are currently propagating the
                // corresponding cancellation exception (it will be correctly re-thrown by the 'catch' block below
                // and overall we will return a cancelled task). But if the underlying operation managed to complete
                // before it was cancelled, we want the entire task to complete as well. This is ok, as the
                // continuation is very lightweight.
                if (resultBuffer is null)
                {
                    return 0;
                }

                WindowsRuntimeIOHelpers.EnsureResultsInUserBuffer(userBuffer, resultBuffer);

                Debug.Assert(resultBuffer.Length <= unchecked(int.MaxValue));

                return unchecked((int)resultBuffer.Length);
            }
            catch (Exception exception)
            {
                // If the interop layer gave us an 'Exception instance', we assume that it hit a general/unknown
                // case, and wrap it into an 'IOException', as this is what 'Stream' users expect.
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();

                return 0;
            }
        }

        // Now that we have validated the state, start the actual read operation
        return ReadCoreAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        // If already cancelled, stop early
        cancellationToken.ThrowIfCancellationRequested();

        if (buffer.IsEmpty)
        {
            return new(0);
        }

        // Fast path: if the memory is backed by an array, use the existing array-based overload directly
        if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment))
        {
            return new(ReadAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken));
        }

        // Helper to perform the actual asynchronous read operation with pinned memory
        async ValueTask<int> ReadPinnedMemoryAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            using MemoryHandle handle = buffer.Pin();

            WindowsRuntimePinnedMemoryBuffer pinnedMemoryBuffer;

            // An explicit unsafe block is needed here because the 'async unsafe' modifier is not supported
            // by the language (CS4004: "Cannot await in an unsafe context"), so we scope the pointer access
            // to just the buffer initialization, which is the only expression that requires unsafe context.
            unsafe
            {
                pinnedMemoryBuffer = new((byte*)handle.Pointer, length: 0, capacity: buffer.Length);
            }

            try
            {
                IInputStream windowsRuntimeStream = (IInputStream)EnsureNotDisposed();

                IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = windowsRuntimeStream.ReadAsync(
                    buffer: pinnedMemoryBuffer,
                    count: pinnedMemoryBuffer.Capacity,
                    options: InputStreamOptions.Partial);

                IBuffer? resultBuffer = await asyncReadOperation.AsTask(cancellationToken).ConfigureAwait(false);

                if (resultBuffer is null)
                {
                    return 0;
                }

                WindowsRuntimeIOHelpers.EnsureResultsInUserBuffer(pinnedMemoryBuffer, resultBuffer);

                Debug.Assert(resultBuffer.Length <= unchecked(int.MaxValue));

                return unchecked((int)resultBuffer.Length);
            }
            catch (Exception exception)
            {
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();

                return 0;
            }
            finally
            {
                pinnedMemoryBuffer.Invalidate();
            }
        }

        // Slow path: pin the memory and use a pinned memory buffer for the async read operation
        return ReadPinnedMemoryAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfInsufficientSpaceInTargetBuffer(buffer.Length, offset, count);
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        if (count == 0)
        {
            return 0;
        }

        // Helper to do a sync-over-async read operation
        StreamReadAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state, bool usedByBlockingWrapper)
        {
            // This method is somewhat tricky: we could consider just calling 'ReadAsync' (recall that 'Task' implements
            // 'IAsyncResult'). It would be ok for cases where 'BeginRead' is invoked directly by the public user.
            // However, in cases where it is invoked by 'Read' to achieve a blocking (synchronous) I/O operation, the
            // 'ReadAsync' approach may result in a deadlock.
            //
            // The sync-over-async I/O operation will be doing a blocking wait on the completion of the asynchronous I/O
            // operation assuming that a wait handle would be signalled by the completion handler. Recall that the 'IAsyncInfo'
            // representing the I/O operation may not be free-threaded and not "free-marshalled". It may also belong to an
            // ASTA compartment because the underlying Windows Runtime stream lives in an ASTA compartment. The completion
            // handler is invoked on a pool thread, i.e. in MTA. That handler needs to fetch the results from the async I/O
            // operation, which requires a cross-compartment call from MTA into ASTA. But because the ASTA thread is busy
            // waiting, this call will deadlock (recall that although 'WaitOne' pumps COM messages, ASTA specifically
            // schedules calls on the outermost idle pump only).
            //
            // The solution is to make sure that:
            //   - In cases where the main thread is waiting for the asynchronous I/O to complete: fetch the results on
            //     the main thread after it has been signalled by the completion callback.
            //   - In cases where the main thread is not waiting for the asynchronous I/O to complete: fetch the results
            //     from the completion callback.
            //
            // But the 'Task' infrastructure around 'IAsyncInfo.AsTask' always fetches results in the completion handler,
            // because it has no way of knowing whether or not someone is waiting. So, instead of using 'ReadAsync' here,
            // we implement our own 'IAsyncResult' and our own completion handler, which can behave differently according
            // to whether it is being used by a blocking I/O operation wrapping a 'BeginRead'/'EndRead' pair, or by an
            // actual asynchronous operation based on the old 'Begin'/'End' pattern.
            IInputStream windowsRuntimeStream = (IInputStream)EnsureNotDisposed();

            IBuffer userBuffer = buffer.AsBuffer(offset, count);

            IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = windowsRuntimeStream.ReadAsync(
                buffer: userBuffer,
                count: unchecked((uint)count),
                options: InputStreamOptions.Partial);

            // The 'StreamReadAsyncResult' object will set a private instance method to act as a 'Completed' handler for
            // the asynchronous operation. This will cause a CCW to be created for the delegate, and the delegate has a
            // reference to its target, i.e. to the 'IAsyncResult' object, which will therefore not be collected. If we
            // lose the entire 'AppDomain', then asynchronous result and its CCW will be collected, but the stub will
            // remain and the callback will fail gracefully. The underlying buffer is the only item to which we expose
            // a direct pointer and this is properly pinned using a mechanism similar to 'Overlapped'.
            return new StreamReadAsyncResult(
                asyncReadOperation,
                userBuffer,
                callback,
                state,
                processCompletedOperationInCallback: !usedByBlockingWrapper);
        }

        IAsyncResult asyncResult = BeginRead(buffer, offset, count, null, null, usedByBlockingWrapper: true);

        int numberOfBytesRead = EndRead(asyncResult);

        return numberOfBytesRead;
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override unsafe int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        if (buffer.IsEmpty)
        {
            return 0;
        }

        IInputStream windowsRuntimeStream = (IInputStream)EnsureNotDisposed();

        // Pin the span so that it stays at the same address while the async I/O operation is in progress.
        // We create a 'WindowsRuntimePinnedMemoryBuffer' wrapping the pinned pointer, then invalidate it
        // in the 'finally' block to ensure no one can access the pointer after the span goes out of scope.
        fixed (byte* pinnedData = buffer)
        {
            WindowsRuntimePinnedMemoryBuffer pinnedMemoryBuffer = new(pinnedData, length: 0, capacity: buffer.Length);

            try
            {
                IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = windowsRuntimeStream.ReadAsync(
                    buffer: pinnedMemoryBuffer,
                    count: unchecked((uint)buffer.Length),
                    options: InputStreamOptions.Partial);

                // See the large comment in the 'Read(byte[], int, int)' method about why we use
                // a custom 'IAsyncResult' implementation instead of 'ReadAsync' + 'AsTask' here.
                StreamReadAsyncResult asyncResult = new(
                    asyncReadOperation,
                    pinnedMemoryBuffer,
                    userCompletionCallback: null,
                    userAsyncStateInfo: null,
                    processCompletedOperationInCallback: false);

                int numberOfBytesRead = EndRead(asyncResult);

                return numberOfBytesRead;
            }
            finally
            {
                pinnedMemoryBuffer.Invalidate();
            }
        }
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override int ReadByte()
    {
        byte result = 0;

        // We don't need to call 'EnsureNotDisposed' here, as it will
        // be will be called from the 'Read' -> 'BeginRead' call chain.
        return Read(new Span<byte>(ref result)) == 0 ? -1 : result;
    }
}
#endif
