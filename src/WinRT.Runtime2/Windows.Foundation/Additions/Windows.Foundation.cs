// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System
{
    using global::System.Diagnostics;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using global::System.Threading;
    using global::System.Threading.Tasks;
    using global::Windows.Foundation;

#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
#if EMBED
    internal
#else 
    public 
#endif 
    static class WindowsRuntimeSystemExtensions
    {
        public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

#if NET
            if (source is ITaskAwareAsyncInfo asyncInfo && asyncInfo.Task is Task task)
            {
                return cancellationToken.CanBeCanceled ?
                    task.WaitAsync(cancellationToken) :
                    task;
            }
#endif

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.CompletedTask;

                case AsyncStatus.Error:
                    return Task.FromException(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            var bridge = new AsyncInfoToTaskBridge<VoidValueTypeParameter, VoidValueTypeParameter>(source, cancellationToken);
            source.Completed = new AsyncActionCompletedHandler(bridge.CompleteFromAsyncAction);
            return bridge.Task;
        }

        public static Task AsTask(this IAsyncAction source)
        {
            return AsTask(source, CancellationToken.None);
        }

        public static TaskAwaiter GetAwaiter(this IAsyncAction source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static void Wait(this IAsyncAction source)
        {
            AsTask(source).Wait();
        }

        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

#if NET
            if (source is ITaskAwareAsyncInfo asyncInfo && asyncInfo.Task is Task<TResult> task)
            {
                return cancellationToken.CanBeCanceled ?
                    task.WaitAsync(cancellationToken) :
                    task;
            }
#endif

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.FromResult(source.GetResults());

                case AsyncStatus.Error:
                    return Task.FromException<TResult>(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            var bridge = new AsyncInfoToTaskBridge<TResult, VoidValueTypeParameter>(source, cancellationToken);
            source.Completed = new AsyncOperationCompletedHandler<TResult>(bridge.CompleteFromAsyncOperation);
            return bridge.Task;
        }

        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source, CancellationToken.None);
        }

        public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static void Wait<TResult>(this IAsyncOperation<TResult> source)
        {
            AsTask(source).Wait();
        }

        public static TResult Get<TResult>(this IAsyncOperation<TResult> source)
        {
            return AsTask(source).Result;
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

#if NET
            // fast path is underlying asyncInfo is Task and no IProgress provided
            if (source is ITaskAwareAsyncInfo asyncInfo && asyncInfo.Task is Task task && progress == null)
            {
                return cancellationToken.CanBeCanceled ?
                    task.WaitAsync(cancellationToken) :
                    task;
            }
#endif

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.CompletedTask;

                case AsyncStatus.Error:
                    return Task.FromException(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            if (progress != null)
            {
                SetProgress(source, progress);
            }

            var bridge = new AsyncInfoToTaskBridge<VoidValueTypeParameter, TProgress>(source, cancellationToken);
            source.Completed = new AsyncActionWithProgressCompletedHandler<TProgress>(bridge.CompleteFromAsyncActionWithProgress);
            return bridge.Task;
        }

        private static void SetProgress<TProgress>(IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> sink)
        {
            // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
            source.Progress = new AsyncActionProgressHandler<TProgress>((_, info) => sink.Report(info));
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            return AsTask(source, CancellationToken.None, null);
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
        {
            return AsTask(source, cancellationToken, null);
        }

        public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
        {
            return AsTask(source, CancellationToken.None, progress);
        }

        public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static void Wait<TProgress>(this IAsyncActionWithProgress<TProgress> source)
        {
            AsTask(source).Wait();
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

#if NET
            // fast path is underlying asyncInfo is Task and no IProgress provided
            if (source is ITaskAwareAsyncInfo asyncInfo && asyncInfo.Task is Task<TResult> task && progress == null)
            {
                return cancellationToken.CanBeCanceled ?
                    task.WaitAsync(cancellationToken) :
                    task;
            }
#endif

            switch (source.Status)
            {
                case AsyncStatus.Completed:
                    return Task.FromResult(source.GetResults());

                case AsyncStatus.Error:
                    return Task.FromException<TResult>(source.ErrorCode);

                case AsyncStatus.Canceled:
                    return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
            }

            if (progress != null)
            {
                SetProgress(source, progress);
            }

            var bridge = new AsyncInfoToTaskBridge<TResult, TProgress>(source, cancellationToken);
            source.Completed = new AsyncOperationWithProgressCompletedHandler<TResult, TProgress>(bridge.CompleteFromAsyncOperationWithProgress);
            return bridge.Task;
        }

        private static void SetProgress<TResult, TProgress>(IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> sink)
        {
            // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
            source.Progress = new AsyncOperationProgressHandler<TResult, TProgress>((_, info) => sink.Report(info));
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source, CancellationToken.None, null);
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken)
        {
            return AsTask(source, cancellationToken, null);
        }

        public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress)
        {
            return AsTask(source, CancellationToken.None, progress);
        }

        public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source).GetAwaiter();
        }

        public static void Wait<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            AsTask(source).Wait();
        }

        public static TResult Get<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
        {
            return AsTask(source).Result;
        }

        public static IAsyncAction AsAsyncAction(this Task source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TaskToAsyncActionAdapter(source, underlyingCancelTokenSource: null);
        }

        public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TaskToAsyncOperationAdapter<TResult>(source, underlyingCancelTokenSource: null);
        }
    }

    // Marker type since generic parameters cannot be 'void'
    struct VoidValueTypeParameter { }

    /// <summary>This can be used instead of <code>VoidValueTypeParameter</code> when a reference type is required.
    /// In case of an actual instantiation (e.g. through <code>default(T)</code>),
    /// using <code>VoidValueTypeParameter</code> offers better performance.</summary>
    internal class VoidReferenceTypeParameter { }

#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
    sealed class AsyncInfoToTaskBridge<TResult, TProgress> : TaskCompletionSource<TResult>
    {
        private readonly CancellationToken _ct;
        private readonly CancellationTokenRegistration _asyncInfoRegistration;
        private readonly CancellationTokenRegistration _registration;

        internal AsyncInfoToTaskBridge(IAsyncInfo asyncInfo, CancellationToken cancellationToken)
        {
            if (asyncInfo == null)
            {
                throw new ArgumentNullException(nameof(asyncInfo));
            }

            this._ct = cancellationToken;
            if (this._ct.CanBeCanceled)
            {
#if NET
                _registration = this._ct.Register(static (b, ct) => ((TaskCompletionSource<TResult>)b).TrySetCanceled(ct), this);
#else
                _registration = this._ct.Register((b) => ((TaskCompletionSource<TResult>)b).TrySetCanceled(this._ct), this);
#endif
                // Handle Exception from Cancel() if the token is already canceled.
                try
                {
                    _asyncInfoRegistration = this._ct.Register(static ai => ((IAsyncInfo)ai).Cancel(), asyncInfo);
                }
                catch (Exception ex)
                {
                    if (!base.Task.IsFaulted)
                    {
                        Debug.Fail($"Expected base task to already be faulted but found it in state {base.Task.Status}");
                        base.TrySetException(ex);
                    }
                }
            }

            // If we're already completed, unregister everything again.  Unregistration is idempotent and thread-safe.
            if (Task.IsCompleted)
            {
                this.Cleanup();
            }
        }
        internal void CompleteFromAsyncAction(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, null, asyncStatus);
        }

        internal void CompleteFromAsyncActionWithProgress(IAsyncActionWithProgress<TProgress> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, null, asyncStatus);
        }

        internal void CompleteFromAsyncOperation(IAsyncOperation<TResult> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, ai => ((IAsyncOperation<TResult>)ai).GetResults(), asyncStatus);
        }

        internal void CompleteFromAsyncOperationWithProgress(IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, AsyncStatus asyncStatus)
        {
            Complete(asyncInfo, ai => ((IAsyncOperationWithProgress<TResult, TProgress>)ai).GetResults(), asyncStatus);
        }

        private void Complete(IAsyncInfo asyncInfo, Func<IAsyncInfo, TResult> getResultsFunction, AsyncStatus asyncStatus)
        {
            if (asyncInfo == null)
            {
                throw new ArgumentNullException(nameof(asyncInfo));
            }

            try
            {
                Debug.Assert(asyncInfo.Status == asyncStatus, "asyncInfo.Status does not match asyncStatus; are we dealing with a faulty IAsyncInfo implementation?");

                if (asyncStatus != AsyncStatus.Completed && asyncStatus != AsyncStatus.Canceled && asyncStatus != AsyncStatus.Error)
                {
                    Debug.Fail("The async operation should be in a terminal state.");
                    throw new InvalidOperationException("The asynchronous operation could not be completed.");
                }

                TResult result = default(TResult);
                Exception error = null;
                if (asyncStatus == AsyncStatus.Error)
                {
                    error = asyncInfo.ErrorCode;

                    // Defend against a faulty IAsyncInfo implementation
                    if (error is null)
                    {
                        Debug.Fail("IAsyncInfo.Status == Error, but ErrorCode returns a null Exception (implying S_OK).");
                        error = new InvalidOperationException("The asynchronous operation could not be completed.");
                    }
                }
                else if (asyncStatus == AsyncStatus.Completed && getResultsFunction != null)
                {
                    try
                    {
                        result = getResultsFunction(asyncInfo);
                    }
                    catch (Exception resultsEx)
                    {
                        // According to the WinRT team, this can happen in some egde cases, such as marshalling errors in GetResults.
                        error = resultsEx;
                        asyncStatus = AsyncStatus.Error;
                    }
                }

                // Complete the task based on the previously retrieved results:
                bool success = false;
                switch (asyncStatus)
                {
                    case AsyncStatus.Completed:
                        success = base.TrySetResult(result);
                        break;

                    case AsyncStatus.Error:
                        Debug.Assert(error != null, "The error should have been retrieved previously.");
                        success = base.TrySetException(error);
                        break;

                    case AsyncStatus.Canceled:
                        success = base.TrySetCanceled(this._ct.IsCancellationRequested ? this._ct : new CancellationToken(true));
                        break;
                }

                if (success)
                {
                    Cleanup();
                }
            }
            catch (Exception exc)
            {
                Debug.Fail($"Unexpected exception in Complete: {exc}");

                if (!base.TrySetException(exc))
                {
                    Debug.Fail("The task was already completed and thus the exception couldn't be stored.");
                    throw;
                }
                else
                {
                    Cleanup();
                }
            }
        }

        private void Cleanup()
        {
            _registration.Dispose();
            _asyncInfoRegistration.Dispose();
        }
    }
}
