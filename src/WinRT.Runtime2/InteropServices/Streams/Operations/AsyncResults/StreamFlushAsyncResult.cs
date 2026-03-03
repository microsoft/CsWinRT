// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A specialized <see cref="StreamOperationAsyncResult"/> implementation for <see cref="Windows.Storage.Streams.IOutputStream.FlushAsync"/>.
/// </summary>
internal sealed class StreamFlushAsyncResult : StreamOperationAsyncResult
{
    /// <summary>
    /// Creates a new <see cref="StreamFlushAsyncResult"/> instance with the specified parameters.
    /// </summary>
    /// <param name="flushAsyncOperation">The asynchronous flush operation to wrap.</param>
    /// <remarks>
    /// This constructor will never cause a completion callback to be invoked.
    /// </remarks>
    public StreamFlushAsyncResult(IAsyncOperation<bool> flushAsyncOperation)
        : base(flushAsyncOperation, null, null, processCompletedOperationInCallback: false)
    {
        flushAsyncOperation.Completed = OnStreamOperationCompleted;
    }

    /// <inheritdoc/>
    protected override void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed)
    {
        // Helper taking an exact 'IAsyncOperation<bool>' instance
        static void ProcessCompletedOperation(IAsyncOperation<bool> completedOperation, out long numberOfBytesProcessed)
        {
            bool success = completedOperation.GetResults();

            // We return '0' or '-1' as placeholders to forward the 'bool' result from the flush operation
            numberOfBytesProcessed = success ? 0 : -1;
        }

        ProcessCompletedOperation((IAsyncOperation<bool>)completedOperation, out numberOfBytesProcessed);
    }
}