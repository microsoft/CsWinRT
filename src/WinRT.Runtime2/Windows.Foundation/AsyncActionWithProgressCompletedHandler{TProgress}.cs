// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous action that provides progress updates.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <param name="asyncInfo">The asynchronous action.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#elif WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
public delegate void AsyncActionWithProgressCompletedHandler<TProgress>(IAsyncActionWithProgress<TProgress> asyncInfo, AsyncStatus asyncStatus);