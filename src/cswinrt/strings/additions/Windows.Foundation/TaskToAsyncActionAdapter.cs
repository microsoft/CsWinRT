// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Threading.Tasks
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Windows.Foundation;

#if NET
    [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
    internal sealed partial class TaskToAsyncActionAdapter
                        : TaskToAsyncInfoAdapter<AsyncActionCompletedHandler, VoidReferenceTypeParameter, VoidValueTypeParameter, VoidValueTypeParameter>,
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
}  // namespace

// TaskToAsyncActionAdapter.cs
