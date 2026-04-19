// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Represents a sequential stream of bytes to be read.
/// </summary>
[Guid("905A0FE2-BC53-11DF-8C49-001E4FC686DA")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(UniversalApiContract), 65536u)]
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
#endif
public interface IInputStream : IDisposable
{
    /// <summary>
    /// Reads data from the stream asynchronously.
    /// </summary>
    /// <param name="buffer">A buffer that may be used to return the bytes that are read. The return value contains the buffer that holds the results.</param>
    /// <param name="count">The number of bytes to read that is less than or equal to the <see cref="IBuffer.Capacity"/> value.</param>
    /// <param name="options">Specifies the type of the asynchronous read operation.</param>
    /// <returns>The asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Always read data from the buffer returned in the <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/>. Don't assume that the
    /// input buffer contains the data. Depending on the implementation, the data that's read might be placed into the input buffer, or it might
    /// be returned in a different buffer. For the input buffer, you don't have to implement the <see cref="IBuffer"/> interface. Instead, you
    /// can create an instance of the <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.buffer"><c>Buffer</c></see> class.
    /// </para>
    /// <para>
    /// Also consider reading a buffer into an <see cref="IInputStream"/> by using the
    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.datareader.readbuffer"><c>ReadBuffer</c></see> method of the
    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.datareader"><c>DataReader</c></see> class.
    /// </para>
    /// </remarks>
    IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options);
}
