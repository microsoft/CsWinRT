// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous operation that can report progress updates to callers. This is the return
/// type for many Windows Runtime asynchronous methods that have results and also report progress.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <remarks>
/// <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> is the return type for many Windows Runtime asynchronous methods that
/// have a result upon completion, and also support notifications that report progress (which callers can subscribe to by assigning a callback
/// for <see cref="Progress"/>). This constitutes about 100 different Windows Runtime APIs. APIs that don't report progress (but do have a result)
/// use another interface, <see cref="IAsyncOperation{TResult}"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperationwithprogress-2"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public interface IAsyncOperationWithProgress<TResult, TProgress> : IAsyncInfo
{
    /// <summary>
    /// Gets or sets the method that handles progress notifications.
    /// </summary>
    AsyncOperationProgressHandler<TResult, TProgress>? Progress { get; set; }

    /// <summary>
    /// Gets or sets the delegate that is called when the operation completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You're not allowed to set the <see cref="Completed"/> property more than once.
    /// </para>
    /// <para>
    /// If the <see cref="Completed"/> property is set after the action has already completed, then the action behaves as if
    /// it had completed immediately after the handler was received. Note that this can result in the handler being called
    /// before the <see cref="Completed"/> property setter has returned; possibly even from the same thread.
    /// </para>
    /// </remarks>
    AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? Completed { get; set; }

    /// <summary>
    /// Returns the results of the operation.
    /// </summary>
    /// <returns>The results of the operation.</returns>
    TResult GetResults();
}
