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
internal sealed partial class TaskToAsyncOperationAdapter<TResult> : TaskToAsyncInfoAdapter<
    TResult,
    VoidValueTypeParameter,
    AsyncOperationCompletedHandler<TResult>,
    VoidReferenceTypeParameter>,
    IAsyncOperation<TResult>
{
    internal TaskToAsyncOperationAdapter(Delegate taskGenerator)

         : base(taskGenerator)
    {
    }


    internal TaskToAsyncOperationAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource)

        : base(underlyingTask, underlyingCancelTokenSource, progress: null)
    {
    }


    internal TaskToAsyncOperationAdapter(TResult synchronousResult)

        : base(synchronousResult)
    {
    }

    internal TaskToAsyncOperationAdapter(bool isCanceled)
        : base(default(TResult))
    {
        if (isCanceled)
            DangerousSetCanceled();
    }

    public TResult GetResults()
    {
        return GetResultsInternal();
    }


    internal override void OnCompleted(AsyncOperationCompletedHandler<TResult> userCompletionHandler, AsyncStatus asyncStatus)
    {
        Debug.Assert(userCompletionHandler != null);
        userCompletionHandler(this, asyncStatus);
    }
}  // class TaskToAsyncOperationAdapter<TResult>
// namespace

// TaskToAsyncOperationAdapter.cs