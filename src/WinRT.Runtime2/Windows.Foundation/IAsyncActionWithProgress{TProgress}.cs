// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous action that can report progress updates to callers. This is the return type for all
/// Windows Runtime asynchronous methods that don't have a result object, but do report progress to callback listeners.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <remarks>
/// <see cref="IAsyncActionWithProgress{TProgress}"/> is the return type for all Windows Runtime asynchronous methods that
/// don't communicate a result object, but do enable an app to check the progress of the action. There aren't nearly as many
/// of these as there are methods that use <see cref="IAsyncAction"/>, whose APIs don't report progress and don't have a result.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public interface IAsyncActionWithProgress<TProgress> : IAsyncInfo
{
    /// <summary>
    /// Gets or sets the callback method that receives progress notification.
    /// </summary>
    AsyncActionProgressHandler<TProgress>? Progress { get; set; }

    /// <summary>
    /// Gets or sets the delegate that is called when the action completes.
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
    AsyncActionWithProgressCompletedHandler<TProgress>? Completed { get; set; }

    /// <summary>
    /// Returns the results of the action.
    /// </summary>
    void GetResults();
}
