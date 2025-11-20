// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Threading.Tasks;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using global::Windows.Foundation;

#if NET
[global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
internal sealed partial class TaskToAsyncActionWithProgressAdapter<TProgress>
                        : TaskToAsyncInfoAdapter<AsyncActionWithProgressCompletedHandler<TProgress>,
                                                 AsyncActionProgressHandler<TProgress>,
                                                 VoidValueTypeParameter,
                                                 TProgress>,
                          IAsyncActionWithProgress<TProgress>
{
    internal TaskToAsyncActionWithProgressAdapter(Delegate taskGenerator)

         : base(taskGenerator)
    {
    }


    // This is currently not used, so commented out to save code.
    // Leaving this is the source to be uncommented in case we decide to support IAsyncActionWithProgress-consturction from a Task.
    //
    //internal TaskToAsyncActionWithProgressAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource,
    //                                                 Progress<TProgress> underlyingProgressDispatcher)
    //
    //    : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher) {
    //}


    internal TaskToAsyncActionWithProgressAdapter(bool isCanceled)

        : base(default(VoidValueTypeParameter))
    {
        if (isCanceled)
            DangerousSetCanceled();
    }


    public void GetResults()
    {
        GetResultsInternal();
    }


    internal override void OnCompleted(AsyncActionWithProgressCompletedHandler<TProgress> userCompletionHandler, AsyncStatus asyncStatus)
    {
        Debug.Assert(userCompletionHandler != null);
        userCompletionHandler(this, asyncStatus);
    }


    internal override void OnProgress(AsyncActionProgressHandler<TProgress> userProgressHandler, TProgress progressInfo)
    {
        Debug.Assert(userProgressHandler != null);
        userProgressHandler(this, progressInfo);
    }
}  // class TaskToAsyncActionWithProgressAdapter<TProgress>
// namespace

// TaskToAsyncActionWithProgressAdapter.cs