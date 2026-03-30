// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="MemoryStream"/> implementation backed by a managed <see cref="IBuffer"/> instance.
/// </summary>
internal sealed class WindowsRuntimeBufferMemoryStream : MemoryStream
{
    /// <summary>
    /// The <see cref="IBuffer"/> instance to back the stream.
    /// </summary>
    private readonly IBuffer _buffer;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeBufferMemoryStream"/> instance with the specified parameters.
    /// </summary>
    /// <param name="buffer">The <see cref="IBuffer"/> instance to back the stream.</param>
    /// <param name="array">The byte array to use as the underlying storage.</param>
    /// <param name="offset">The offset in the byte array where the stream starts.</param>
    /// <remarks>This constructor doesn't validate any of its parameters.</remarks>
    public WindowsRuntimeBufferMemoryStream(IBuffer buffer, byte[] array, int offset)
        : base(array, offset, (int)buffer.Capacity, writable: true)
    {
        _buffer = buffer;

        SetLength(buffer.Length);
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        base.SetLength(value);

        // The input value is limited by 'Capacity', so this cast is safe
        _buffer.Length = (uint)Length;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        base.Write(buffer, offset, count);

        _buffer.Length = (uint)Length;
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        base.Write(buffer);

        _buffer.Length = (uint)Length;
    }

    /// <inheritdoc/>
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await base.WriteAsync(buffer, offset, count, cancellationToken);

        _buffer.Length = (uint)Length;
    }

    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await base.WriteAsync(buffer, cancellationToken);

        _buffer.Length = (uint)Length;
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        base.WriteByte(value);

        _buffer.Length = (uint)Length;
    }
}
#endif