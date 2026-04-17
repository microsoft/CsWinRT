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
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Foundation.AsyncActionCompletedHandler>")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(FoundationContract), 65536u)]
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[ABI.Windows.Foundation.AsyncActionCompletedHandlerComWrappersMarshaller]
#endif
public delegate void AsyncActionCompletedHandler(IAsyncAction asyncInfo, AsyncStatus asyncStatus);