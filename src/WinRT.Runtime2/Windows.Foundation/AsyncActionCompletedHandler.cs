// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous operation.
/// </summary>
/// <param name="asyncInfo">The asynchronous operation.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
#if !REFERENCE_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Foundation.AsyncActionCompletedHandler>")]
#endif
[ContractVersion(typeof(FoundationContract), 65536u)]
#if !REFERENCE_ASSEMBLY
[ABI.Windows.Foundation.AsyncActionCompletedHandlerComWrappersMarshaller]
#endif
public delegate void AsyncActionCompletedHandler(IAsyncAction asyncInfo, AsyncStatus asyncStatus);