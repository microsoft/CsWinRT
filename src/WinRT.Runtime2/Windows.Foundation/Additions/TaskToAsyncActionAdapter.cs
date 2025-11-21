// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Threading.Tasks;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using global::Windows.Foundation;

[global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
internal sealed partial class TaskToAsyncActionAdapter : TaskToAsyncInfoAdapter<
    VoidValueTypeParameter,
    VoidValueTypeParameter,
    AsyncActionCompletedHandler,
    VoidReferenceTypeParameter>,
    IAsyncAction
{
    internal TaskToAsyncActionAdapter(Delegate taskGenerator)

         : base(taskGenerator)
    {
    }


    internal TaskToAsyncActionAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource)

        : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher: null)
    {
    }


    internal TaskToAsyncActionAdapter(bool isCanceled)

        : base(default(VoidValueTypeParameter))
    {
        if (isCanceled)
            DangerousSetCanceled();
    }


    public void GetResults()
    {
        GetResultsInternal();
    }


    internal override void OnCompleted(AsyncActionCompletedHandler userCompletionHandler, AsyncStatus asyncStatus)
    {
        Debug.Assert(userCompletionHandler != null);
        userCompletionHandler(this, asyncStatus);
    }
}  // class TaskToAsyncActionAdapter
// namespace

// TaskToAsyncActionAdapter.cs