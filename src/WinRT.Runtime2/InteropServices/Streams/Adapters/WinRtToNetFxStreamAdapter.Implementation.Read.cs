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
    public override int EndRead(IAsyncResult asyncResult)
    {
        ArgumentNullException.ThrowIfNull(asyncResult);

        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream is null);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

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
        ArgumentException.ThrowIfInsufficientSpaceInTargetBuffer(buffer.Length, offset, count);
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream is null);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        // If already cancelled, bail early:
        cancellationToken.ThrowIfCancellationRequested();

        async Task<int> ReadCoreAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            IInputStream wrtStr = (IInputStream)EnsureNotDisposed();

            try
            {
                IBuffer userBuffer = buffer.AsBuffer(offset, count);

                IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = wrtStr.ReadAsync(
                    buffer: userBuffer,
                    count: unchecked((uint)count),
                    options: InputStreamOptions.Partial);

                IBuffer? resultBuffer = await asyncReadOperation.AsTask(cancellationToken).ConfigureAwait(false);

                // If cancellationToken was cancelled until now, then we are currently propagating the corresponding cancellation exception.
                // (It will be correctly rethrown by the catch block below and overall we will return a cancelled task.)
                // But if the underlying operation managed to complete before it was cancelled, we want
                // the entire task to complete as well. This is ok as the continuation is very lightweight:

                if (resultBuffer is null)
                {
                    return 0;
                }

                WindowsRuntimeIOHelpers.EnsureResultsInUserBuffer(userBuffer, resultBuffer);

                Debug.Assert(resultBuffer.Length <= unchecked(int.MaxValue));

                return unchecked((int)resultBuffer.Length);
            }
            catch (Exception ex)
            {
                // If the interop layer gave us an Exception, we assume that it hit a general/unknown case and wrap it into
                // an IOException as this is what Stream users expect.
                WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(ex).Throw();
                return 0;
            }
        }

        // State is Ok. Do the actual read:
        return ReadCoreAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentException.ThrowIfInsufficientSpaceInTargetBuffer(buffer.Length, offset, count);
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream is null);
        NotSupportedException.ThrowIfStreamCannotRead(_canRead);

        StreamReadAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state, bool usedByBlockingWrapper)
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
            IInputStream wrtStr = (IInputStream)EnsureNotDisposed();

            IBuffer userBuffer = buffer.AsBuffer(offset, count);

            IAsyncOperationWithProgress<IBuffer, uint> asyncReadOperation = wrtStr.ReadAsync(
                buffer: userBuffer,
                count: unchecked((uint)count),
                options: InputStreamOptions.Partial);

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
}