// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous action. This is the return type for many Windows Runtime
/// asynchronous methods that don't have a result object, and don't report ongoing progress.
/// </summary>
/// <remarks>
/// The <see cref="IAsyncAction"/> interface represents an asynchronous action that does not return a result and does
/// not have progress notifications. When the action completes, the <see cref="AsyncActionCompletedHandler"/> specified
/// by <see cref="Completed"/> is invoked, and this is the only result from the action.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncaction"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IAsyncAction")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[IAsyncActionComWrappersMarshaller]
public interface IAsyncAction : IAsyncInfo
{
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
    AsyncActionCompletedHandler? Completed { get; set; }

    /// <summary>
    /// Returns the results of the action.
    /// </summary>
    void GetResults();
}
