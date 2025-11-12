// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using Windows.Foundation.Metadata;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents the status for an asynchronous operation.
/// </summary>
/// <remarks>
/// This type is required for ABI projection of the <see cref="IAsyncInfo"/> interface, but marshalling it is not supported.
/// </remarks>
/// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/ne-asyncinfo-asyncstatus"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public enum AsyncStatus
{
    /// <summary>
    /// The operation is in progress.
    /// </summary>
    Started = 0,

    /// <summary>
    /// The operation has completed without error.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The client has initiated a cancellation of the operation.
    /// </summary>
    Canceled = 2,

    /// <summary>
    /// The operation has completed with an error. No results are available.
    /// </summary>
    Error = 3
}
