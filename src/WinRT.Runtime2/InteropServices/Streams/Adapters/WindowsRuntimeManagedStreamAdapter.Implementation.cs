// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeManagedStreamAdapter"/>
internal partial class WindowsRuntimeManagedStreamAdapter
{
    /// <inheritdoc/>
    public override bool CanRead => _canRead && _windowsRuntimeStream is not null;

    /// <inheritdoc/>
    public override bool CanWrite => _canWrite && _windowsRuntimeStream is not null;

    /// <inheritdoc/>
    public override bool CanSeek => _canSeek && _windowsRuntimeStream is not null;

    /// <inheritdoc/>
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
            NotSupportedException.ThrowIfStreamCannotUseLength(_canSeek);

            IRandomAccessStream windowsRuntimeStream = (IRandomAccessStream)EnsureNotDisposed();

            ulong size = windowsRuntimeStream.Size;

            IOException.ThrowIfUnderlyingWinRTStreamTooLong(size);

            return unchecked((long)size);
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
            NotSupportedException.ThrowIfStreamCannotUsePosition(_canSeek);

            IRandomAccessStream windowsRuntimeStream = (IRandomAccessStream)EnsureNotDisposed();

            ulong position = windowsRuntimeStream.Position;

            IOException.ThrowIfUnderlyingWinRTStreamTooLong(position);

            return unchecked((long)position);
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeStreamPosition(value);
            ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
            NotSupportedException.ThrowIfStreamCannotUsePosition(_canSeek);

            IRandomAccessStream windowsRuntimeStream = (IRandomAccessStream)EnsureNotDisposed();

            windowsRuntimeStream.Seek(unchecked((ulong)value));
        }
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeStreamLength(value);
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotSeek(_canSeek);
        NotSupportedException.ThrowIfStreamCannotWrite(_canWrite);

        IRandomAccessStream windowsRuntimeStream = (IRandomAccessStream)EnsureNotDisposed();

        windowsRuntimeStream.Size = unchecked((ulong)value);

        // If the length is set to a value less than the current position, then we need to
        // set the position to that value. This is because if we can't directly set the
        // position, we are going to seek to it.
        if (windowsRuntimeStream.Size < windowsRuntimeStream.Position)
        {
            windowsRuntimeStream.Seek(unchecked((ulong)value));
        }
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIfStreamIsDisposed(_windowsRuntimeStream);
        NotSupportedException.ThrowIfStreamCannotSeek(_canSeek);

        IRandomAccessStream windowsRuntimeStream = (IRandomAccessStream)EnsureNotDisposed();

        // Helper for seeking from the start of the stream
        static long ComputeBeginSeek(long offset) => offset;

        // Helper for seeking relative to the current position in the stream
        long ComputeCurrentSeek(long offset)
        {
            long currentPosition = Position;

            IOException.ThrowIfSeekWouldExceedMaxPosition(currentPosition, offset);

            long newPosition = currentPosition + offset;

            IOException.ThrowIfSeekResultNegative(newPosition);

            return newPosition;
        }

        // Helper for seeking from the end of the stream
        long ComputeEndSeek(long offset, IRandomAccessStream windowsRuntimeStream)
        {
            ulong sizeNative = windowsRuntimeStream.Size;
            long newPosition;

            // If the current size exceeds 'long.MaxValue', then we can only seek when the input offset
            // is negative, because that way there's a chance the final position might still be valid.
            if (sizeNative > long.MaxValue)
            {
                IOException.ThrowIfNonNegativeOffsetForOversizedStream(offset);

                ulong absoluteOffset = (offset == long.MinValue) ? ((ulong)long.MaxValue) + 1 : (ulong)-offset;

                Debug.Assert(absoluteOffset <= sizeNative);

                ulong newPositionNative = sizeNative - absoluteOffset;

                IOException.ThrowIfSeekPositionExceedsInt64MaxValue(newPositionNative);

                newPosition = (long)newPositionNative;
            }
            else
            {
                // Otherwise, we just need to update the position makin sure we don't overflow
                long size = unchecked((long)sizeNative);

                IOException.ThrowIfSeekWouldExceedMaxPosition(size, offset);

                newPosition = size + offset;

                IOException.ThrowIfSeekResultNegative(newPosition);
            }

            return newPosition;
        }

        long position = origin switch
        {
            SeekOrigin.Begin => ComputeBeginSeek(offset),
            SeekOrigin.Current => ComputeCurrentSeek(offset),
            SeekOrigin.End => ComputeEndSeek(offset, windowsRuntimeStream),
            _ => throw ArgumentException.GetInvalidSeekOriginException(nameof(origin))
        };

        Position = position;

        return position;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // Dispose the underlying native stream, if needed
        if (disposing && _windowsRuntimeStream is not null && _disposeNativeStream)
        {
            // All Windows Runtime streams should implement 'IDisposable', but let's be defensive
            IDisposable? disposable = _windowsRuntimeStream as IDisposable;

            disposable?.Dispose();
        }

        _windowsRuntimeStream = null;

        base.Dispose(disposing);
    }
}
#endif
