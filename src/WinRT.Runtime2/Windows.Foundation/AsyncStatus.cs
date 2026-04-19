// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System.Runtime.Versioning;
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents the status for an asynchronous operation.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/ne-asyncinfo-asyncstatus"/>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Foundation.AsyncStatus>")]
[WindowsRuntimeReferenceType(typeof(AsyncStatus?))]
#elif WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[ABI.Windows.Foundation.AsyncStatusComWrappersMarshaller]
#endif
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