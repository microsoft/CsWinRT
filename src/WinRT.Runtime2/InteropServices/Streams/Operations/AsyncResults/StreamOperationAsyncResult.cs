// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable IDE0032

namespace System.IO
{
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Threading;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;
    using System.Diagnostics.CodeAnalysis;
    using WindowsRuntime.InteropServices;

    internal abstract class StreamOperationAsyncResult : IAsyncResult
    {
        private readonly AsyncCallback? _userCompletionCallback;
        private readonly object? _userAsyncStateInfo;
        private readonly bool _processCompletedOperationInCallback;

        private IAsyncInfo? _asyncStreamOperation;

        private volatile bool _completed;
        private volatile bool _callbackInvoked;
        private volatile ManualResetEvent? _waitHandle;

        private long _bytesCompleted = 0;

        private ExceptionDispatchInfo? _errorInfo;
        private IAsyncInfo? _completedOperation;

        protected StreamOperationAsyncResult(
            IAsyncInfo asyncStreamOperation,
            AsyncCallback? userCompletionCallback,
            object? userAsyncStateInfo,
            bool processCompletedOperationInCallback)
        {
            _userCompletionCallback = userCompletionCallback;
            _userAsyncStateInfo = userAsyncStateInfo;
            _processCompletedOperationInCallback = processCompletedOperationInCallback;
            _asyncStreamOperation = asyncStreamOperation;
        }

        /// <summary>
        /// Finalizes the current operation object.
        /// </summary>
        ~StreamOperationAsyncResult()
        {
            // This finalisation is not critical (we're not directly responsible for any unmanaged resources),
            // but we can still make an effort to notify the underlying Windows Runtime stream object that we
            // are not any longer interested in the results of the operation.
            _ = CancelStreamOperation();
        }

        /// <inheritdoc/>
        public object? AsyncState => _userAsyncStateInfo;

        /// <inheritdoc/>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                ManualResetEvent? waitHandle = _waitHandle;

                // Just return the existing handle if we have already created one
                if (waitHandle is not null)
                {
                    return waitHandle;
                }

                // Create a new wait handle passing the flag indicating whether the current operation
                // has completed. This ensures things will work fine if someone will retrieve this
                // wait handle through the public property and then wait on it. That is, if the task
                // is already completed, the wait handle will also already be in the signaled state.
                waitHandle = new ManualResetEvent(_completed);

                // Try to atomically set the new wait handle
                ManualResetEvent? otherHandle = Interlocked.CompareExchange(ref _waitHandle, waitHandle, null);

                // If the original value was not 'null', it means we raced against another thread
                // and lost. In that case, we can just dispose this handle and return the other one.
                if (otherHandle is not null)
                {
                    waitHandle.Dispose();

                    return otherHandle;
                }

                // If we got here, we won the race, so we can directly return our local wait handle
                return waitHandle;
            }
        }

        /// <inheritdoc/>
        public bool CompletedSynchronously => false;

        /// <inheritdoc/>
        public bool IsCompleted => _completed;

        /// <inheritdoc/>
        public bool ProcessCompletedOperationInCallback => _processCompletedOperationInCallback;

        public long BytesCompleted => _bytesCompleted;

        [MemberNotNullWhen(true, nameof(_errorInfo))]
        public bool HasError => _errorInfo != null;

        public void Wait()
        {
            if (_completed)
            {
                return;
            }

            WaitHandle wh = AsyncWaitHandle;

            // Keep waiting on the handle until the current task reaches the completed state
            while (!_completed)
            {
                _ = wh.WaitOne();
            }
        }

        public void ThrowCachedError()
        {
            _errorInfo?.Throw();
        }

        public void CloseStreamOperation()
        {
            try
            {
                _asyncStreamOperation?.Close();
            }
            catch
            {
            }

            _asyncStreamOperation = null;
        }

        private bool CancelStreamOperation()
        {
            if (_callbackInvoked)
            {
                return false;
            }

            _asyncStreamOperation?.Cancel();
            _asyncStreamOperation = null;

            return true;
        }

        protected abstract void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed);

        private static void ProcessCompletedOperation_InvalidOperationThrowHelper(ExceptionDispatchInfo errInfo, string errMsg)
        {
            Exception errInfosrc = (errInfo == null) ? null : errInfo.SourceException;

            if (errInfosrc == null)
                throw new InvalidOperationException(errMsg);
            else
                throw new InvalidOperationException(errMsg, errInfosrc);
        }


        internal void ProcessCompletedOperation()
        {
            // The error handling is slightly tricky here:
            // Before processing the IO results, we are verifying some basic assumptions and if they do not hold, we are
            // throwing InvalidOperation. However, by the time this method is called, we might have already stored something
            // into errorInfo, e.g. if an error occurred in StreamOperationCompletedCallback. If that is the case, then that
            // previous exception might include some important info relevant for detecting the problem. So, we take that
            // previous exception and attach it as the inner exception to the InvalidOperationException being thrown.
            // In cases where we have a good understanding of the previously saved errorInfo, and we know for sure that it
            // the immediate reason for the state validation to fail, we can avoid throwing InvalidOperation altogether
            // and only rethrow the errorInfo.

            if (!_callbackInvoked)
                ProcessCompletedOperation_InvalidOperationThrowHelper(_errorInfo, global::Windows.Storage.Streams.SR.InvalidOperation_CannotCallThisMethodInCurrentState);

            if (!_processCompletedOperationInCallback && !_completed)
                ProcessCompletedOperation_InvalidOperationThrowHelper(_errorInfo, global::Windows.Storage.Streams.SR.InvalidOperation_CannotCallThisMethodInCurrentState);

            if (_completedOperation == null)
            {
                ExceptionDispatchInfo errInfo = _errorInfo;
                Exception errInfosrc = (errInfo == null) ? null : errInfo.SourceException;

                // See if errorInfo is set because we observed completedOperation == null previously (being slow is Ok on error path):
                if (errInfosrc != null && errInfosrc is NullReferenceException
                        && global::Windows.Storage.Streams.SR.NullReference_IOCompletionCallbackCannotProcessNullAsyncInfo.Equals(errInfosrc.Message))
                {
                    errInfo!.Throw();
                }
                else
                {
                    throw new InvalidOperationException(global::Windows.Storage.Streams.SR.InvalidOperation_CannotCallThisMethodInCurrentState);
                }
            }

            if (_completedOperation.Id != _asyncStreamOperation!.Id)
                ProcessCompletedOperation_InvalidOperationThrowHelper(_errorInfo, global::Windows.Storage.Streams.SR.InvalidOperation_UnexpectedAsyncOperationID);

            if (_completedOperation.Status == AsyncStatus.Error)
            {
                _bytesCompleted = 0;
                ThrowWithIOExceptionDispatchInfo(_completedOperation.ErrorCode);
            }

            ProcessCompletedOperation(_completedOperation, out _bytesCompleted);
        }

        internal void StreamOperationCompletedCallback(IAsyncInfo completedOperation, AsyncStatus unusedCompletionStatus)
        {
            try
            {
                if (_callbackInvoked)
                    throw new InvalidOperationException(global::Windows.Storage.Streams.SR.InvalidOperation_MultipleIOCompletionCallbackInvocation);

                _callbackInvoked = true;

                // This happens in rare stress cases in Console mode and the WinRT folks said they are unlikely to fix this in Dev11.
                // Moreover, this can happen if the underlying WinRT stream has a faulty user implementation.
                // If we did not do this check, we would either get the same exception without the explaining message when dereferencing
                // completedOperation later, or we will get an InvalidOperation when processing the Op. With the check, they will be
                // aggregated and the user will know what went wrong.
                if (completedOperation == null)
                    throw new NullReferenceException(global::Windows.Storage.Streams.SR.NullReference_IOCompletionCallbackCannotProcessNullAsyncInfo);

                _completedOperation = completedOperation;

                // processCompletedOperationInCallback == false indicates that the stream is doing a blocking wait on the waitHandle of this IAsyncResult.
                // In that case calls on completedOperation may deadlock if completedOperation is not free threaded.
                // By setting processCompletedOperationInCallback to false the stream that created this IAsyncResult indicated that it
                // will call ProcessCompletedOperation after the waitHandle is signalled to fetch the results.

                if (_processCompletedOperationInCallback)
                    ProcessCompletedOperation();
            }
            catch (Exception ex)
            {
                _bytesCompleted = 0;
                _errorInfo = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                _completed = true;
                Interlocked.MemoryBarrier();
                // From this point on, AsyncWaitHandle would create a handle that is readily set,
                // so we do not need to check if it is being produced asynchronously.
                if (_waitHandle != null)
                    _waitHandle.Set();
            }

            if (_userCompletionCallback != null)
                _userCompletionCallback(this);
        }

        private void ThrowWithIOExceptionDispatchInfo(Exception e)
        {
            WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(RestrictedErrorInfo.AttachErrorInfo(_completedOperation.ErrorCode)).Throw();
        }
    }

    internal sealed class StreamReadAsyncResult : StreamOperationAsyncResult
    {
        /// <summary>
        /// The user-provided <see cref="IBuffer"/> instance to use for the read operation.
        /// </summary>
        private readonly IBuffer _userBuffer;

        public StreamReadAsyncResult(
            IAsyncOperationWithProgress<IBuffer, uint> asyncStreamReadOperation,
            IBuffer buffer,
            AsyncCallback userCompletionCallback,
            object userAsyncStateInfo,
            bool processCompletedOperationInCallback)
            : base(asyncStreamReadOperation, userCompletionCallback, userAsyncStateInfo, processCompletedOperationInCallback)
        {
            Debug.Assert(asyncStreamReadOperation is not null);

            _userBuffer = buffer;

            asyncStreamReadOperation.Completed = StreamOperationCompletedCallback;
        }

        /// <inheritdoc/>
        protected override void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed)
        {
            // Helper taking an exact 'IAsyncOperationWithProgress<IBuffer, uint>' instance
            void ProcessCompletedOperation(IAsyncOperationWithProgress<IBuffer, uint> completedOperation, out long bytesCompleted)
            {
                IBuffer resultBuffer = completedOperation.GetResults();

                Debug.Assert(resultBuffer is not null);

                WindowsRuntimeIOHelpers.EnsureResultsInUserBuffer(_userBuffer, resultBuffer);

                bytesCompleted = _userBuffer.Length;
            }

            ProcessCompletedOperation((IAsyncOperationWithProgress<IBuffer, uint>)completedOperation, out numberOfBytesProcessed);
        }
    }

    internal sealed class StreamWriteAsyncResult : StreamOperationAsyncResult
    {
        internal StreamWriteAsyncResult(
            IAsyncOperationWithProgress<uint, uint> asyncStreamWriteOperation,
            AsyncCallback userCompletionCallback,
            object userAsyncStateInfo,
            bool processCompletedOperationInCallback)
            : base(asyncStreamWriteOperation, userCompletionCallback, userAsyncStateInfo, processCompletedOperationInCallback)
        {
            asyncStreamWriteOperation.Completed = StreamOperationCompletedCallback;
        }

        /// <inheritdoc/>
        protected override void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed)
        {
            // Helper taking an exact 'IAsyncOperationWithProgress<uint, uint>' instance
            static void ProcessCompletedOperation(IAsyncOperationWithProgress<uint, uint> completedOperation, out long numberOfBytesProcessed)
            {
                uint numberOfBytesWritten = completedOperation.GetResults();

                numberOfBytesProcessed = numberOfBytesWritten;
            }

            ProcessCompletedOperation((IAsyncOperationWithProgress<uint, uint>)completedOperation, out numberOfBytesProcessed);
        }
    }

    internal sealed class StreamFlushAsyncResult : StreamOperationAsyncResult
    {
        internal StreamFlushAsyncResult(IAsyncOperation<bool> asyncStreamFlushOperation, bool processCompletedOperationInCallback)
            : base(asyncStreamFlushOperation, null, null, processCompletedOperationInCallback)
        {
            asyncStreamFlushOperation.Completed = StreamOperationCompletedCallback;
        }

        /// <inheritdoc/>
        protected override void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed)
        {
            // Helper taking an exact 'IAsyncOperation<bool>' instance
            static void ProcessCompletedOperation(IAsyncOperation<bool> completedOperation, out long numberOfBytesProcessed)
            {
                bool success = completedOperation.GetResults();

                // We return '0' or '-1' as placeholders to forward the 'bool' result from the flush operation
                numberOfBytesProcessed = success ? 0 : -1;
            }

            ProcessCompletedOperation((IAsyncOperation<bool>)completedOperation, out numberOfBytesProcessed);
        }
    }
}