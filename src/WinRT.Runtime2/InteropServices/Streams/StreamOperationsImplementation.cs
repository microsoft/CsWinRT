// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Tasks;
using Windows.Storage.Buffers;
using WindowsRuntime.InteropServices;

#pragma warning disable CS1573

namespace Windows.Storage.Streams;

/// <summary>
/// Provides methods that encapsulate specialized logic to handle <see cref="Stream"/> operations when exposed to the Windows Runtime.
/// </summary>
/// <remarks>
/// Depending on the concrete type of the underlying stream managed by a <see cref="NetFxToWinRtStreamAdapter"/> instance, we want the
/// <see cref="IInputStream.ReadAsync"/>, <see cref="IOutputStream.WriteAsync"/>, and <see cref="IOutputStream.FlushAsync"/> methods to
/// be implemented differently. This is for best performance, as we can take advantage of the specifics of particular stream types. For
/// instance, <see cref="IInputStream.ReadAsync"/> currently has a special implementation for <see cref="MemoryStream"/> objects. Also,
/// knowledge about the actual runtime type of the <see cref="IBuffer"/> being used can also help choosing the optimal implementation.
/// </remarks>
[SupportedOSPlatform("windows10.0.10240.0")]
internal static class StreamOperationsImplementation
{
    /// <summary>
    /// Specialized methods for <see cref="MemoryStream"/>.
    /// </summary>
    public static class MemoryStream
    {
        /// <inheritdoc cref="IInputStream.ReadAsync"/>
        /// <param name="stream">The underlying managed <see cref="Stream"/> instance to read data from.</param>
        /// <remarks>
        /// This method is specialized for <paramref name="stream"/> being a <see cref="System.IO.MemoryStream"/> instance.
        /// </remarks>
        public static IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(Stream stream, IBuffer buffer, uint count)
        {
            Debug.Assert(stream is not null);
            Debug.Assert(stream is System.IO.MemoryStream);
            Debug.Assert(stream.CanRead);
            Debug.Assert(stream.CanSeek);
            Debug.Assert(buffer is not null);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= int.MaxValue);
            Debug.Assert(count <= buffer.Capacity);

            // We will return a different buffer to the user, backed directly by the input 'MemoryStream' object (avoids a memory copy).
            // This is permitted by the Windows Runtime 'IInputStream' contract. The user specified buffer will not have any data put
            // into it, so we can just reset the length of that buffer to '0'.
            buffer.Length = 0;

            System.IO.MemoryStream memoryStream = (System.IO.MemoryStream)stream;

            try
            {
                IBuffer dataBuffer = memoryStream.GetWindowsRuntimeBuffer((int)memoryStream.Position, (int)count);

                if (dataBuffer.Length > 0)
                {
                    _ = memoryStream.Seek(dataBuffer.Length, SeekOrigin.Current);
                }

                return AsyncInfo.FromResultWithProgress<IBuffer, uint>(dataBuffer);
            }
            catch (Exception ex)
            {
                return AsyncInfo.FromExceptionWithProgress<IBuffer, uint>(ex);
            }
        }
    }

    /// <inheritdoc cref="IInputStream.ReadAsync"/>
    /// <param name="stream">The underlying managed <see cref="Stream"/> instance to read data from.</param>
    /// <remarks>
    /// This method works with <paramref name="stream"/> being of any type.
    /// </remarks>
    public static IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(
        Stream stream,
        IBuffer buffer,
        uint count,
        InputStreamOptions options)
    {
        Debug.Assert(stream is not null);
        Debug.Assert(stream.CanRead);
        Debug.Assert(buffer is not null);
        Debug.Assert(count >= 0);
        Debug.Assert(count <= int.MaxValue);
        Debug.Assert(count <= buffer.Capacity);
        Debug.Assert(Enum.IsDefined(options));

        int numberOfBytesToRead = (int)count;

        // Check if the buffer is our implementation.
        // IF YES: In that case, we can read directly into its data array.
        // IF NO:  The buffer is of unknown implementation. It's not backed by a managed array, but the wrapped stream can only
        //         read into a managed array. If we used the user-supplied buffer we would need to copy data into it after every read.
        //         The spec allows to return a buffer instance that is not the same as passed by the user. So, we will create an own
        //         buffer instance, read data *directly* into the array backing it and then return it to the user.
        //         Note: the allocation costs we are paying for the new buffer are unavoidable anyway, as we would need to create
        //         an array to read into either way.
        //
        // TODO: optimize this for external buffers too by using a custom 'Memory<byte>' implementation
        IBuffer dataBuffer = buffer is WindowsRuntimePinnedArrayBuffer or WindowsRuntimeExternalArrayBuffer
            ? buffer
            : WindowsRuntimeBuffer.Create((int)uint.Min(buffer.Capacity, (uint)Array.MaxLength));

        // Helper with the core read operation logic, that will run inside the returned 'IAsyncOperationWithProgress<IBuffer, uint>' instance
        async Task<IBuffer> ReadCoreAsync(CancellationToken token, IProgress<uint> progress)
        {
            // Reset the length of the target buffer, since we haven't read any bytes yet
            dataBuffer.Length = 0;

            // Get the buffer backing array to read data into
            bool managedBufferAssert = WindowsRuntimeBufferHelpers.TryGetManagedArray(dataBuffer, out byte[]? data, out int offset);

            Debug.Assert(managedBufferAssert); // TODO: remove this after refactoring code above

            bool shouldStop = token.IsCancellationRequested;
            int numberOfTotalBytesRead = 0;

            // Loop until the end of stream is reached, the token is cancelled, or we read enough data according to the input options
            while (!shouldStop)
            {
                int numberOfPartialBytesRead = 0;

                try
                {
                    // Read the stream data asynchronously
                    numberOfPartialBytesRead = await stream.ReadAsync(
                        buffer: data!,
                        offset: offset + numberOfTotalBytesRead,
                        count: numberOfBytesToRead - numberOfTotalBytesRead,
                        cancellationToken: token).ConfigureAwait(false); // TODO: switch this to 'Memory<byte>'

                    // We might continue on a different thread here after the read has completed (this is intentional)
                    numberOfTotalBytesRead += numberOfPartialBytesRead;
                }
                catch (OperationCanceledException)
                {
                    Debug.Assert(token.IsCancellationRequested);

                    // If the cancellation came after we read some bytes already, we want to return the results we got instead
                    // of an empty cancelled 'Task'. So we only re-throw this cancellation if we haven't read anything at all.
                    if (numberOfTotalBytesRead == 0 && numberOfPartialBytesRead == 0)
                    {
                        throw;
                    }
                }

                Debug.Assert(numberOfTotalBytesRead <= numberOfBytesToRead);

                // Update target buffer
                dataBuffer.Length = (uint)numberOfTotalBytesRead;

                // Check if we should stop reading or proceed to do another iteration:
                //   - If the user requested a partial read, we can stop after any successful read operation, even if
                //     we haven't read all requested bytes yet. Basically, any amount of data being read is valid.
                //   - If we read '0' bytes in the last iteration, it means we reached the end of the stream.
                //   - If we read all the requested bytes, then we are done as well.
                //   - Finally, if the cancellation token was cancelled, we should also stop. Note that in this case,
                //     like mentioned above, if we have read some data we'll return that instead of just throwing.
                shouldStop =
                    options == InputStreamOptions.Partial ||
                    numberOfPartialBytesRead == 0 ||
                    numberOfTotalBytesRead == numberOfBytesToRead ||
                    token.IsCancellationRequested;

                // Call the user-provided progress handler
                progress.Report(dataBuffer.Length);
            }

            // If we got here, then no error was detected, so we can return the results
            return dataBuffer;
        }

        return AsyncInfo.Run<IBuffer, uint>(ReadCoreAsync);
    }

    /// <inheritdoc cref="IOutputStream.WriteAsync"/>
    /// <param name="stream">The underlying managed <see cref="Stream"/> instance to write data to.</param>
    public static IAsyncOperationWithProgress<uint, uint> WriteAsync(Stream stream, IBuffer buffer)
    {
        Debug.Assert(stream is not null);
        Debug.Assert(stream.CanWrite);
        Debug.Assert(buffer is not null);

        // Check if the buffer is backed by a managed array we can extract
        if (WindowsRuntimeBufferHelpers.TryGetManagedArray(buffer, out byte[]? data, out int offset))
        {
            // Helper method to create the wrapped operation (we extract this here to try to reduce
            // codegen pessimizations due to the closure, which Roslyn tends to eagerly allocate).
            static IAsyncOperationWithProgress<uint, uint> WriteManagedArrayAsync(
                Stream stream,
                IBuffer buffer,
                byte[] data,
                int offset)
            {
                // Helper with the core write logic from the unwrapped managed array we just retrieved
                async Task<uint> WriteManagedArrayCoreAsync(CancellationToken token, IProgress<uint> progress)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }

                    Debug.Assert(buffer.Length <= int.MaxValue);

                    int numberOfBytesToWrite = (int)buffer.Length;

                    await stream.WriteAsync(data, offset, numberOfBytesToWrite, token).ConfigureAwait(false);

                    progress.Report((uint)numberOfBytesToWrite);

                    return (uint)numberOfBytesToWrite;
                }

                return AsyncInfo.Run<uint, uint>(WriteManagedArrayCoreAsync);
            }

            return WriteManagedArrayAsync(stream, buffer, data, offset);
        }

        // Helper to create the wrapped operation (same as above, but only for native buffers)
        static IAsyncOperationWithProgress<uint, uint> WriteNativeBufferAsync(Stream stream, IBuffer buffer)
        {
            // Helper with the core write logic from a native buffer (i.e. not backed by a managed array)
            async Task<uint> WriteNativeBufferCoreAsync(CancellationToken token, IProgress<uint> progress)
            {
                if (token.IsCancellationRequested)
                {
                    return 0;
                }

                // Pick the right buffer size for the write operation ('0x4000' is an arbitrary default buffer size)
                uint numberOfBytesToWrite = buffer.Length;
                int bufferSize = (int)uint.Min(numberOfBytesToWrite, 0x4000);

                // Perform the actual write operation only if we actually have any data to write
                if (bufferSize > 0)
                {
                    using Stream dataStream = buffer.AsStream();

                    await dataStream.CopyToAsync(stream, bufferSize, token).ConfigureAwait(false);
                }

                progress.Report(numberOfBytesToWrite);

                return numberOfBytesToWrite;
            }

            return AsyncInfo.Run<uint, uint>(WriteNativeBufferCoreAsync);
        }

        return WriteNativeBufferAsync(stream, buffer);
    }

    /// <inheritdoc cref="IOutputStream.FlushAsync"/>
    /// <param name="stream">The underlying managed <see cref="Stream"/> instance to flush.</param>
    public static IAsyncOperation<bool> FlushAsync(Stream stream)
    {
        Debug.Assert(stream is not null);
        Debug.Assert(stream.CanWrite);

        // Helper with the core flush logic (this will always return 'true' unless an exception is thrown)
        async Task<bool> FlushCoreAsync(CancellationToken cancelToken)
        {
            if (cancelToken.IsCancellationRequested)
            {
                return false;
            }

            await stream.FlushAsync(cancelToken).ConfigureAwait(false);

            return true;
        }

        return AsyncInfo.Run(FlushCoreAsync);
    }
}
