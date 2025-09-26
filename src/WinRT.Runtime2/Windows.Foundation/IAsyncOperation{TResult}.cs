// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous operation, which returns a result upon completion. This is the return
/// type for many Windows Runtime asynchronous methods that have results but don't report progress.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <remarks>
/// <see cref="IAsyncOperation{TResult}"/> is the return type for many Windows Runtime asynchronous methods that have a result
/// upon completion, but don't report progress. This constitutes over 650 different Windows Runtime APIs. APIs that report
/// progress and have a result use another interface, <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public interface IAsyncOperation<TResult> : IAsyncInfo
{
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
    AsyncOperationCompletedHandler<TResult>? Completed { get; set; }

    /// <summary>
    /// Returns the results of the operation.
    /// </summary>
    /// <returns>The results of the operation.</returns>
    TResult GetResults();
}
