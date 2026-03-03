// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using Windows.Foundation;
using Windows.Storage.Streams;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A specialized <see cref="StreamOperationAsyncResult"/> implementation for <see cref="IInputStream.ReadAsync"/>.
/// </summary>
internal sealed class StreamReadAsyncResult : StreamOperationAsyncResult
{
    /// <summary>
    /// The user-provided <see cref="IBuffer"/> instance to use for the read operation.
    /// </summary>
    private readonly IBuffer _userBuffer;

    /// <inheritdoc cref="StreamOperationAsyncResult.StreamOperationAsyncResult"/>
    /// <summary>
    /// Creates a new <see cref="StreamReadAsyncResult"/> instance with the specified parameters.
    /// </summary>
    /// <param name="readAsyncOperation">The asynchronous read operation to wrap.</param>
    /// <param name="buffer">The user-provided <see cref="IBuffer"/> instance to use for the read operation.</param>
    public StreamReadAsyncResult(
        IAsyncOperationWithProgress<IBuffer, uint> readAsyncOperation,
        IBuffer buffer,
        AsyncCallback userCompletionCallback,
        object userAsyncStateInfo,
        bool processCompletedOperationInCallback)
        : base(readAsyncOperation, userCompletionCallback, userAsyncStateInfo, processCompletedOperationInCallback)
    {
        Debug.Assert(readAsyncOperation is not null);

        _userBuffer = buffer;

        readAsyncOperation.Completed = OnStreamOperationCompleted;
    }

    /// <inheritdoc/>
    protected override void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed)
    {
        // Helper taking an exact 'IAsyncOperationWithProgress<IBuffer, uint>' instance
        void ProcessCompletedOperation(IAsyncOperationWithProgress<IBuffer, uint> completedOperation, out long bytesCompleted)
        {
            IBuffer resultBuffer = completedOperation.GetResults();

            Debug.Assert(resultBuffer is not null);

            WindowsRuntimeIOHelpers.EnsureResultsInUserBuffer(_userBuffer, resultBuffer);

            bytesCompleted = _userBuffer.Length;
        }

        ProcessCompletedOperation((IAsyncOperationWithProgress<IBuffer, uint>)completedOperation, out numberOfBytesProcessed);
    }
}