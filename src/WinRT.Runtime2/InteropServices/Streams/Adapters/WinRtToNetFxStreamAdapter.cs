// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Tasks;
using Windows.Storage.Buffers;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <code>Stream</code> used to wrap a Windows Runtime stream to expose it as a managed steam.
/// </summary>
internal sealed class WinRtToNetFxStreamAdapter : Stream, IDisposable
{
    /// <summary>
    /// The Windows Runtime stream being wrapped
    /// </summary>
    private object? _windowsRuntimeStream;

    /// <summary>
    /// Indicates whether <see cref="_windowsRuntimeStream"/> is a readable stream.
    /// </summary>
    private readonly bool _canRead;

    /// <summary>
    /// Indicates whether <see cref="_windowsRuntimeStream"/> is a writeable stream.
    /// </summary>
    private readonly bool _canWrite;

    /// <summary>
    /// Indicates whether <see cref="_windowsRuntimeStream"/> is a stream supporting seeking.
    /// </summary>
    private readonly bool _canSeek;

    /// <summary>
    /// Indicates whether to dispose <see cref="_windowsRuntimeStream"/> when <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    private bool _disposeNativeStream;

    /// <summary>
    /// Creates a new <see cref="WinRtToNetFxStreamAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream instance to wrap.</param>
    /// <param name="canRead">Indicates whether <paramref name="windowsRuntimeStream"/> is a readable stream.</param>
    /// <param name="canWrite">Indicates whether <paramref name="windowsRuntimeStream"/> is a writeable stream.</param>
    /// <param name="canSeek">Indicates whether <paramref name="windowsRuntimeStream"/> is a stream supporting seeking.</param>
    private WinRtToNetFxStreamAdapter(object windowsRuntimeStream, bool canRead, bool canWrite, bool canSeek)
    {
        Debug.Assert(windowsRuntimeStream is not null);
        Debug.Assert(windowsRuntimeStream is IInputStream or IOutputStream or IRandomAccessStream);
        Debug.Assert(canSeek == (windowsRuntimeStream is IRandomAccessStream));

        // If a stream is readable, it must be an 'IInputStream', or if not it must either not be one, or explicitly have 'CanRead' be 'false'
        Debug.Assert((canRead && (windowsRuntimeStream is IInputStream)) ||
                     (!canRead && (windowsRuntimeStream is not IInputStream or IRandomAccessStream { CanRead: false })));

        // If a stream is writeable, it must be an 'IOutputStream', or if not it must either not be one, or explicitly have 'CanWrite' be 'false'
        Debug.Assert((canWrite && (windowsRuntimeStream is IOutputStream)) ||
                     (!canWrite && (windowsRuntimeStream is not IOutputStream or IRandomAccessStream { CanWrite: false })));

        _windowsRuntimeStream = windowsRuntimeStream;
        _canRead = canRead;
        _canWrite = canWrite;
        _canSeek = canSeek;
    }

    /// <summary>
    /// Creates a new <see cref="WinRtToNetFxStreamAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream instance to wrap.</param>
    /// <remarks>
    /// The <paramref name="windowsRuntimeStream"/> object must implement at least one of the following Windows Runtime
    /// interfaces: <see cref="IInputStream"/>, <see cref="IOutputStream"/>, or <see cref="IRandomAccessStream"/>.
    /// </remarks>
    public static WinRtToNetFxStreamAdapter Create(object windowsRuntimeStream)
    {
        Debug.Assert(windowsRuntimeStream is not null);

        bool canRead = windowsRuntimeStream is IInputStream;
        bool canWrite = windowsRuntimeStream is IOutputStream;
        bool canSeek = windowsRuntimeStream is IRandomAccessStream;

        // If we can't perform any operations on the input stream, then it's invalid
        if (!canRead && !canWrite && !canSeek)
        {
            throw new ArgumentException(SR.Argument_ObjectMustBeWinRtStreamToConvertToNetFxStream);
        }

        // Proactively guard against a non-conforming implementations
        if (canSeek)
        {
            IRandomAccessStream randomAccessStream = (IRandomAccessStream)windowsRuntimeStream;

            if (!canRead && randomAccessStream.CanRead)
            {
                throw new ArgumentException(SR.Argument_InstancesImplementingIRASThatCanReadMustImplementIIS);
            }

            if (!canWrite && randomAccessStream.CanWrite)
            {
                throw new ArgumentException(SR.Argument_InstancesImplementingIRASThatCanWriteMustImplementIOS);
            }

            // If we have an 'IRandomAccessStream' instance, its 'CanRead' property takes precedence here.
            // This is because the stream would also implement 'IInputStream' (because it's a base interface
            // of 'IRandomAccessStream'), but it doesn't mean it can actually be read from in this case.
            if (!randomAccessStream.CanRead)
            {
                canRead = false;
            }

            if (!randomAccessStream.CanWrite)
            {
                canWrite = false;
            }
        }

        // Check again that we can perform some useful operations. We repeat this check here
        // in case we have an 'IRandomAccessStream' that specifies it can't do any of these.
        if (!canRead && !canWrite)
        {
            throw new ArgumentException(SR.Argument_WinRtStreamCannotReadOrWrite);
        }

        // Create the managed wrapper implementation around the input Windows Runtime stream
        return new WinRtToNetFxStreamAdapter(windowsRuntimeStream, canRead, canWrite, canSeek);
    }

    #region Tools and Helpers

    /// <inheritdoc cref="NetFxToWinRtStreamAdapter.SetWonInitializationRace"/>
    public void SetWonInitializationRace()
    {
        _disposeNativeStream = true;
    }

    /// <summary>
    /// Gets the underlying Windows Runtime stream, if the current instance has not been disposed.
    /// </summary>
    /// <returns>The underlying Windows Runtime stream, if available.</returns>
    public object? GetWindowsRuntimeStream()
    {
        return _windowsRuntimeStream;
    }

    /// <summary>
    /// Asserts that the current instance has not been disposed, and fails if it was.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the current instance has been disposed.</exception>
    private void AssertNotDisposed()
    {
        object? windowsRuntimeStream = _windowsRuntimeStream;

        if (windowsRuntimeStream is null)
        {
            throw new ObjectDisposedException(SR.ObjectDisposed_CannotPerformOperation);
        }
    }

    /// <summary>
    /// Ensures that the current instance has not been disposed and returns a valid stream instance.
    /// </summary>
    /// <returns>The underlying Windows Runtime stream if the current instance has not been disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the current instance has been disposed.</exception>
    [MemberNotNull(nameof(_windowsRuntimeStream))]
    private object EnsureNotDisposed()
    {
        object? windowsRuntimeStream = _windowsRuntimeStream;

        if (windowsRuntimeStream is null)
        {
            throw new ObjectDisposedException(SR.ObjectDisposed_CannotPerformOperation);
        }

        // Same suppression as in 'NetFxToWinRtStreamAdapter.EnsureNotDisposed'
#pragma warning disable CS8774
        return windowsRuntimeStream;
#pragma warning restore CS8774
    }

    private void EnsureCanRead()
    {
        if (!_canRead)
        {
            throw new NotSupportedException(SR.NotSupported_CannotReadFromStream);
        }
    }

    private void EnsureCanWrite()
    {
        if (!_canWrite)
        {
            throw new NotSupportedException(SR.NotSupported_CannotWriteToStream);
        }
    }

    #endregion Tools and Helpers


    #region Simple overrides

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // Dispose the underlying native stream, if needed
        if (disposing && _windowsRuntimeStream is not null && _disposeNativeStream)
        {
            // All Windows Runtime streams should implement 'IDisposable', but let's be defensive
            IDisposable? disposable = _windowsRuntimeStream as IDisposable;

            disposable?.Dispose();
        }

        _windowsRuntimeStream = null;

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override bool CanRead => _canRead && _windowsRuntimeStream is not null;

    /// <inheritdoc/>
    public override bool CanWrite => _canWrite && _windowsRuntimeStream is not null;

    /// <inheritdoc/>
    public override bool CanSeek => _canSeek && _windowsRuntimeStream is not null;

    #endregion Simple overrides


    #region Length and Position functions

    /// <inheritdoc/>
    public override long Length
    {
        get
        {
            IRandomAccessStream wrtStr = (IRandomAccessStream)EnsureNotDisposed();

            if (!_canSeek)
            {
                throw new NotSupportedException(SR.NotSupported_CannotUseLength_StreamNotSeekable);
            }

            Debug.Assert(wrtStr != null);

            ulong size = wrtStr.Size;

            // These are over 8000 PetaBytes, we do not expect this to happen. However, let's be defensive:
            if (size > long.MaxValue)
            {
                throw new IOException(SR.IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition);
            }

            return unchecked((long)size);
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get
        {
            IRandomAccessStream wrtStr = (IRandomAccessStream)EnsureNotDisposed();

            if (!_canSeek)
            {
                throw new NotSupportedException(SR.NotSupported_CannotUsePosition_StreamNotSeekable);
            }

            Debug.Assert(wrtStr != null);

            ulong pos = wrtStr.Position;

            // These are over 8000 PetaBytes, we do not expect this to happen. However, let's be defensive:
            if (pos > long.MaxValue)
            {
                throw new IOException(SR.IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition);
            }

            return unchecked((long)pos);
        }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Position), SR.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);
            }

            IRandomAccessStream wrtStr = (IRandomAccessStream)EnsureNotDisposed();

            if (!_canSeek)
            {
                throw new NotSupportedException(SR.NotSupported_CannotUsePosition_StreamNotSeekable);
            }

            Debug.Assert(wrtStr != null);

            wrtStr.Seek(unchecked((ulong)value));
        }
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        IRandomAccessStream wrtStr = (IRandomAccessStream)EnsureNotDisposed();

        if (!_canSeek)
        {
            throw new NotSupportedException(SR.NotSupported_CannotSeekInStream);
        }

        switch (origin)
        {
            case SeekOrigin.Begin:
                {
                    Position = offset;
                    return offset;
                }

            case SeekOrigin.Current:
                {
                    long curPos = Position;

                    if (long.MaxValue - curPos < offset)
                    {
                        throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);
                    }

                    long newPos = curPos + offset;

                    if (newPos < 0)
                    {
                        throw new IOException(SR.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);
                    }

                    Position = newPos;
                    return newPos;
                }

            case SeekOrigin.End:
                {
                    ulong size = wrtStr.Size;
                    long newPos;

                    if (size > long.MaxValue)
                    {
                        if (offset >= 0)
                        {
                            throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);
                        }

                        Debug.Assert(offset < 0);

                        ulong absOffset = (offset == long.MinValue) ? ((ulong)long.MaxValue) + 1 : (ulong)(-offset);
                        Debug.Assert(absOffset <= size);

                        ulong np = size - absOffset;
                        if (np > long.MaxValue)
                        {
                            throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);
                        }

                        newPos = (long)np;
                    }
                    else
                    {
                        Debug.Assert(size <= long.MaxValue);

                        long s = unchecked((long)size);

                        if (long.MaxValue - s < offset)
                        {
                            throw new IOException(SR.IO_CannotSeekBeyondInt64MaxValue);
                        }

                        newPos = s + offset;

                        if (newPos < 0)
                        {
                            throw new IOException(SR.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);
                        }
                    }

                    Position = newPos;
                    return newPos;
                }

            default:
                {
                    throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
                }
        }
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_CannotResizeStreamToNegative);
        }

        IRandomAccessStream wrtStr = (IRandomAccessStream)EnsureNotDisposed();

        if (!_canSeek)
        {
            throw new NotSupportedException(SR.NotSupported_CannotSeekInStream);
        }

        EnsureCanWrite();

        Debug.Assert(wrtStr != null);

        wrtStr.Size = unchecked((ulong)value);

        // If the length is set to a value < that the current position, then we need to set the position to that value
        // Because we can't directly set the position, we are going to seek to it.
        if (wrtStr.Size < wrtStr.Position)
        {
            wrtStr.Seek(unchecked((ulong)value));
        }
    }

    #endregion Length and Position functions


    #region Reading

    [SupportedOSPlatform("windows10.0.10240.0")]
    private StreamReadAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state, bool usedByBlockingWrapper)
    {
        // This method is somewhat tricky: We could consider just calling ReadAsync (recall that Task implements IAsyncResult).
        // It would be OK for cases where BeginRead is invoked directly by the public user.
        // However, in cases where it is invoked by Read to achieve a blocking (synchronous) IO operation, the ReadAsync-approach may deadlock:
        //
        // The sync-over-async IO operation will be doing a blocking wait on the completion of the async IO operation assuming that
        // a wait handle would be signalled by the completion handler. Recall that the IAsyncInfo representing the IO operation may
        // not be free-threaded and not "free-marshalled"; it may also belong to an ASTA compartment because the underlying WinRT
        // stream lives in an ASTA compartment. The completion handler is invoked on a pool thread, i.e. in MTA.
        // That handler needs to fetch the results from the async IO operation, which requires a cross-compartment call from MTA into ASTA.
        // But because the ASTA thread is busy waiting this call will deadlock.
        // (Recall that although WaitOne pumps COM, ASTA specifically schedules calls on the outermost ?idle? pump only.)
        //
        // The solution is to make sure that:
        //  - In cases where main thread is waiting for the async IO to complete:
        //    Fetch results on the main thread after it has been signalled by the completion callback.
        //  - In cases where main thread is not waiting for the async IO to complete:
        //    Fetch results in the completion callback.
        //
        // But the Task-plumbing around IAsyncInfo.AsTask *always* fetches results in the completion handler because it has
        // no way of knowing whether or not someone is waiting. So, instead of using ReadAsync here we implement our own IAsyncResult
        // and our own completion handler which can behave differently according to whether it is being used by a blocking IO
        // operation wrapping a BeginRead/EndRead pair, or by an actual async operation based on the old Begin/End pattern.

        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (buffer.Length - offset < count)
        {
            throw new ArgumentException(SR.Argument_InsufficientSpaceInTargetBuffer);
        }

        IInputStream wrtStr = (IInputStream)EnsureNotDisposed();
        EnsureCanRead();

        Debug.Assert(wrtStr != null);

        IBuffer userBuffer = buffer.AsBuffer(offset, count);
        IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = wrtStr.ReadAsync(userBuffer,
                                                                                           unchecked((uint)count),
                                                                                           InputStreamOptions.Partial);

        // The StreamReadAsyncResult will set a private instance method to act as a Completed handler for asyncOperation.
        // This will cause a CCW to be created for the delegate and the delegate has a reference to its target, i.e. to
        // asyncResult, so asyncResult will not be collected. If we loose the entire AppDomain, then asyncResult and its CCW
        // will be collected but the stub will remain and the callback will fail gracefully. The underlying buffer is the only
        // item to which we expose a direct pointer and this is properly pinned using a mechanism similar to Overlapped.
        return new StreamReadAsyncResult(
            asyncReadOperation,
            userBuffer,
            callback,
            state,
            processCompletedOperationInCallback: !usedByBlockingWrapper);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override int EndRead(IAsyncResult asyncResult)
    {
        ArgumentNullException.ThrowIfNull(asyncResult);

        AssertNotDisposed();
        EnsureCanRead();

        StreamOperationAsyncResult streamAsyncResult = asyncResult as StreamOperationAsyncResult;
        if (streamAsyncResult == null)
        {
            throw new ArgumentException(SR.Argument_UnexpectedAsyncResult, nameof(asyncResult));
        }

        streamAsyncResult.Wait();

        try
        {
            // If the async result did NOT process the async IO operation in its completion handler (i.e. check for errors,
            // cache results etc), then we need to do that processing now. This is to allow blocking-over-async IO operations.
            // See the big comment in BeginRead for details.

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

            // Done:

            long bytesCompleted = streamAsyncResult.NumberOfBytesProcessed;
            Debug.Assert(bytesCompleted <= unchecked(int.MaxValue));

            return (int)bytesCompleted;
        }
        finally
        {
            // Closing multiple times is Ok.
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

        if (buffer.Length - offset < count)
        {
            throw new ArgumentException(SR.Argument_InsufficientSpaceInTargetBuffer);
        }

        AssertNotDisposed();
        EnsureCanRead();

        // If already cancelled, bail early:
        cancellationToken.ThrowIfCancellationRequested();

        // State is Ok. Do the actual read:
        return ReadAsyncInternal(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        // Arguments validation and not-disposed validation are done in BeginRead.

        IAsyncResult asyncResult = BeginRead(buffer, offset, count, null, null, usedByBlockingWrapper: true);
        int bytesread = EndRead(asyncResult);
        return bytesread;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        byte result = 0;

        // We don't need to call 'EnsureNotDisposed' here, as it will
        // be will be called from the 'Read' -> 'BeginRead' call chain.
        return Read(new Span<byte>(ref result)) == 0 ? -1 : result;
    }

    #endregion Reading


    #region Writing

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

        if (buffer.Length - offset < count)
        {
            throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);
        }

        IOutputStream wrtStr = (IOutputStream)EnsureNotDisposed();
        EnsureCanWrite();

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

        AssertNotDisposed();
        EnsureCanWrite();

        StreamOperationAsyncResult streamAsyncResult = asyncResult as StreamOperationAsyncResult;
        if (streamAsyncResult == null)
        {
            throw new ArgumentException(SR.Argument_UnexpectedAsyncResult, nameof(asyncResult));
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

        if (buffer.Length - offset < count)
        {
            throw new ArgumentException(SR.Argument_InsufficientArrayElementsAfterOffset);
        }

        IOutputStream wrtStr = (IOutputStream)EnsureNotDisposed();
        EnsureCanWrite();

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

    #endregion Writing


    #region Flushing

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

    #endregion Flushing


    #region ReadAsyncInternal implementation
    // Moved it to the end while using Dev10 VS because it does not understand async and everything that follows looses intellisense.
    // Should move this code into the Reading regios once using Dev11 VS becomes the norm.

    [SupportedOSPlatform("windows10.0.10240.0")]
    private async Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Debug.Assert(buffer != null);
        Debug.Assert(offset >= 0);
        Debug.Assert(count >= 0);
        Debug.Assert(buffer.Length - offset >= count);
        Debug.Assert(_canRead);

        IInputStream wrtStr = (IInputStream)EnsureNotDisposed();

        try
        {
            IBuffer userBuffer = buffer.AsBuffer(offset, count);
            IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = wrtStr.ReadAsync(userBuffer,
                                                                                               unchecked((uint)count),
                                                                                               InputStreamOptions.Partial);

            IBuffer resultBuffer = await asyncReadOperation.AsTask(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            // If cancellationToken was cancelled until now, then we are currently propagating the corresponding cancellation exception.
            // (It will be correctly rethrown by the catch block below and overall we will return a cancelled task.)
            // But if the underlying operation managed to complete before it was cancelled, we want
            // the entire task to complete as well. This is ok as the continuation is very lightweight:

            if (resultBuffer == null)
            {
                return 0;
            }

            WindowsRuntimeIOHelpers.EnsureResultsInUserBuffer(userBuffer, resultBuffer);

            Debug.Assert(resultBuffer.Length <= unchecked(int.MaxValue));
            return (int)resultBuffer.Length;
        }
        catch (Exception ex)
        {
            // If the interop layer gave us an Exception, we assume that it hit a general/unknown case and wrap it into
            // an IOException as this is what Stream users expect.
            WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(ex).Throw();
            return 0;
        }
    }
    #endregion ReadAsyncInternal implementation

}