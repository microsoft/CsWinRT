// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents the status for an asynchronous operation.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/ne-asyncinfo-asyncstatus"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Foundation.AsyncStatus>")]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[ABI.Windows.Foundation.AsyncStatusComWrappersMarshaller]
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