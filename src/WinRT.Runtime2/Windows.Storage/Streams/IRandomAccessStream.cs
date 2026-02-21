// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Supports random access of data in input and output streams.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
[Guid("905A0FE1-BC53-11DF-8C49-001E4FC686DA")]
[ContractVersion(typeof(UniversalApiContract), 65536u)]
public interface IRandomAccessStream : IDisposable, IInputStream, IOutputStream
{
    /// <summary>
    /// Gets a value that indicates whether the stream can be read from.
    /// </summary>
    bool CanRead { get; }

    /// <summary>
    /// Gets a value that indicates whether the stream can be written to.
    /// </summary>
    bool CanWrite { get; }

    /// <summary>
    /// Gets the byte offset of the stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The initial offset of an <see cref="IRandomAccessStream"/> is <c>0</c>.
    /// </para>
    /// <para>
    /// This offset is affected by both <see cref="IInputStream"/> and <see cref="IOutputStream"/> operations.
    /// </para>
    /// </remarks>
    ulong Position { get; }

    /// <summary>
    /// Gets or sets the size of the random access stream.
    /// </summary>
    ulong Size { get; set; }

    /// <summary>
    /// Returns an input stream at a specified location in a stream.
    /// </summary>
    /// <param name="position">The location in the stream at which to begin.</param>
    /// <returns>The input stream.</returns>
    IInputStream GetInputStreamAt(ulong position);

    /// <summary>
    /// Returns an output stream at a specified location in a stream.
    /// </summary>
    /// <param name="position">The location in the output stream at which to begin.</param>
    /// <returns>The output stream.</returns>
    IOutputStream GetOutputStreamAt(ulong position);

    /// <summary>
    /// Sets the position of the stream to the specified value.
    /// </summary>
    /// <param name="position">The new position of the stream.</param>
    /// <remarks>
    /// This method does not check the position to make sure the value is valid for the stream. If the position is
    /// invalid for the stream, the <see cref="IInputStream.ReadAsync"/> and <see cref="IOutputStream.WriteAsync"/>
    /// methods will return an error when called.
    /// </remarks>
    void Seek(ulong position);

    /// <summary>
    /// Creates a new instance of an <see cref="IRandomAccessStream"/> over the same resource as the current stream.
    /// </summary>
    /// <returns>The new stream. The initial, internal position of the stream is <c>0</c>.</returns>
    /// <remarks>
    /// The internal position and lifetime of this new stream are independent from the position and lifetime of the cloned stream.
    /// </remarks>
    IRandomAccessStream CloneStream();
}
