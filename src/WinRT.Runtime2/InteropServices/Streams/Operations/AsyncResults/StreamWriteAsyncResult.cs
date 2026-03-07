// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using Windows.Foundation;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A specialized <see cref="StreamOperationAsyncResult"/> implementation for <see cref="Windows.Storage.Streams.IOutputStream.WriteAsync"/>.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class StreamWriteAsyncResult : StreamOperationAsyncResult
{
    /// <inheritdoc cref="StreamOperationAsyncResult.StreamOperationAsyncResult"/>
    /// <summary>
    /// Creates a new <see cref="StreamWriteAsyncResult"/> instance with the specified parameters.
    /// </summary>
    /// <param name="writeAsyncOperation">The asynchronous write operation to wrap.</param>
    internal StreamWriteAsyncResult(
        IAsyncOperationWithProgress<uint, uint> writeAsyncOperation,
        AsyncCallback? userCompletionCallback,
        object? userAsyncStateInfo,
        bool processCompletedOperationInCallback)
        : base(writeAsyncOperation, userCompletionCallback, userAsyncStateInfo, processCompletedOperationInCallback)
    {
        writeAsyncOperation.Completed = OnStreamOperationCompleted;
    }

    /// <inheritdoc/>
    protected override void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed)
    {
        // Helper taking an exact 'IAsyncOperationWithProgress<uint, uint>' instance
        static void ProcessCompletedOperation(IAsyncOperationWithProgress<uint, uint> completedOperation, out long numberOfBytesProcessed)
        {
            uint numberOfBytesWritten = completedOperation.GetResults();

            numberOfBytesProcessed = numberOfBytesWritten;
        }

        ProcessCompletedOperation((IAsyncOperationWithProgress<uint, uint>)completedOperation, out numberOfBytesProcessed);
    }
}