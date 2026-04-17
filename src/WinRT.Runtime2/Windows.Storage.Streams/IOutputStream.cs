// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Represents a sequential stream of bytes to be written.
/// </summary>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
#endif
[Guid("905A0FE6-BC53-11DF-8C49-001E4FC686DA")]
[ContractVersion(typeof(UniversalApiContract), 65536u)]
public interface IOutputStream : IDisposable
{
    /// <summary>
    /// Writes data asynchronously in a sequential stream.
    /// </summary>
    /// <param name="buffer">A buffer that contains the data to be written.</param>
    /// <returns>
    /// The byte writer operation. The first integer represents the number of bytes written.
    /// The second integer represents the progress of the write operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Some stream implementations support queuing of write operations. In this case, the asynchronous execution of the <see cref="WriteAsync"/> method does not complete until
    /// the <see cref="FlushAsync"/> method has completed. For the buffer parameter, you don't have to implement the <see cref="IBuffer"/> interface. Instead, you can create an
    /// instance of the <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.buffer"><c>Buffer</c></see> class or create a buffer by using methods in the
    /// <see href="https://learn.microsoft.com/uwp/api/windows.security.cryptography.cryptographicbuffer"><c>CryptographicBuffer</c></see> class.
    /// </para>
    /// <para>
    /// Also consider writing a buffer into an <see cref="IOutputStream"/> by using the
    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.datareader.writebuffer"><c>WriteBuffer</c></see> method of the
    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.datawriter"><c>DataWriter</c></see> class.
    /// </para>
    /// </remarks>
    IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer);

    /// <summary>
    /// Flushes data asynchronously in a sequential stream.
    /// </summary>
    /// <returns>The stream flush operation.</returns>
    /// <remarks>
    /// The <see cref="FlushAsync"/> method improves, but does not guarantee durable and coherent storage of data,
    /// and it introduces latencies. It's generally recommended to avoid this method to achieve good performance,
    /// but it should be used if coherency is desired.
    /// </remarks>
    IAsyncOperation<bool> FlushAsync();
}
