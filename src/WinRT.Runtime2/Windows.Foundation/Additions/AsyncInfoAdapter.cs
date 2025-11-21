// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A base type for all <see cref="Windows.Foundation.IAsyncInfo"/> adapter types.
/// </summary>
internal abstract class AsyncInfoAdapter
{
    /// <summary>
    /// Gets the underlying <see cref="Task"/> instance for this adapter.
    /// </summary>
    /// <remarks>
    /// The resulting <see cref="System.Threading.Tasks.Task"/> instance may be
    /// <see langword="null"/> if the operation completed synchronously.
    /// </remarks>
    public abstract Task? Task { get; }
}