// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Provides support for asynchronous operations.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public interface IAsyncInfo
{
    /// <summary>
    /// Retrieves the identifier of the asynchronous operation.
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Gets a value that indicates the status of the asynchronous operation.
    /// </summary>
    AsyncStatus Status { get; }

    /// <summary>
    /// Retrieves the termination status of the asynchronous operation.
    /// </summary>
    Exception? ErrorCode { get; }

    /// <summary>
    /// Requests cancellation of the asynchronous operation already in progress.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Closes the asynchronous work object.
    /// </summary>
    void Close();
}
