// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles progress update events of an asynchronous action that provides progress updates.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <param name="asyncInfo">The asynchronous action.</param>
/// <param name="progressInfo">The progress information.</param>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
[ContractVersion(typeof(FoundationContract), 65536u)]
public delegate void AsyncActionProgressHandler<TProgress>(IAsyncActionWithProgress<TProgress> asyncInfo, TProgress progressInfo);