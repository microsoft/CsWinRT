// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous operation.
/// </summary>
/// <param name="asyncInfo">The asynchronous operation.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.AsyncActionCompletedHandler>")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[AsyncActionCompletedHandlerComWrappersMarshaller]
public delegate void AsyncActionCompletedHandler(IAsyncAction asyncInfo, AsyncStatus asyncStatus);
