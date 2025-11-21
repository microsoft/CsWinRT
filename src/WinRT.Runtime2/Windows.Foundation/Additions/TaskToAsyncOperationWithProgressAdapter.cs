// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Threading.Tasks;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using global::Windows.Foundation;

#if NET
[global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
internal sealed partial class TaskToAsyncOperationWithProgressAdapter<TResult, TProgress> : TaskToAsyncInfoAdapter<
    TResult,
    TProgress,
    AsyncOperationWithProgressCompletedHandler<TResult, TProgress>,
    AsyncOperationProgressHandler<TResult, TProgress>>,
    IAsyncOperationWithProgress<TResult, TProgress>
{
    internal TaskToAsyncOperationWithProgressAdapter(Delegate taskGenerator)

         : base(taskGenerator)
    {
    }

    // This is currently not used, so commented out to save code.
    // Leaving this is the source to be uncommented in case we decide to support IAsyncOperationWithProgress-consturction from a Task.
    //
    //internal TaskToAsyncOperationWithProgressAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource,
    //                                                 Progress<TProgress> underlyingProgressDispatcher)
    //
    //    : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher) {
    //}


    internal TaskToAsyncOperationWithProgressAdapter(TResult synchronousResult)

        : base(synchronousResult)
    {
    }

    internal TaskToAsyncOperationWithProgressAdapter(bool isCanceled)
        : base(default(TResult))
    {
        if (isCanceled)
            DangerousSetCanceled();
    }

    public TResult GetResults()
    {
        return GetResultsInternal();
    }


    internal override void OnCompleted(AsyncOperationWithProgressCompletedHandler<TResult, TProgress> userCompletionHandler,
                                       AsyncStatus asyncStatus)
    {
        Debug.Assert(userCompletionHandler != null);
        userCompletionHandler(this, asyncStatus);
    }


    internal override void OnProgress(AsyncOperationProgressHandler<TResult, TProgress> userProgressHandler, TProgress progressInfo)
    {
        Debug.Assert(userProgressHandler != null);
        userProgressHandler(this, progressInfo);
    }
}  // class TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>
// namespace

// TaskToAsyncOperationWithProgressAdapter.cs