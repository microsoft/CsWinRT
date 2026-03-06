// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeNativeStreamAdapter"/>
internal partial class WindowsRuntimeNativeStreamAdapter
{
    /// <inheritdoc cref="IRandomAccessStream.CanRead"/>
    public bool CanRead
    {
        get
        {
            Stream managedStream = EnsureNotDisposed();

            return managedStream.CanRead;
        }
    }

    /// <inheritdoc cref="IRandomAccessStream.CanWrite"/>
    public bool CanWrite
    {
        get
        {
            Stream managedStream = EnsureNotDisposed();

            return managedStream.CanWrite;
        }
    }

    /// <inheritdoc cref="IRandomAccessStream.Position"/>
    public ulong Position
    {
        get
        {
            Stream managedStream = EnsureNotDisposed();

            return (ulong)managedStream.Position;
        }
    }

    /// <inheritdoc cref="IRandomAccessStream.Size"/>
    public ulong Size
    {
        get
        {
            Stream managedStream = EnsureNotDisposed();

            return (ulong)managedStream.Length;
        }
        set
        {
            ArgumentException.ThrowIfSizeExceedsInt64MaxValue(value);

            Stream managedStream = EnsureNotDisposed();

            InvalidOperationException.ThrowIfStreamCannotWriteForResize(managedStream);

            Debug.Assert(managedStream.CanSeek);
            Debug.Assert(value <= long.MaxValue);

            managedStream.SetLength(unchecked((long)value));
        }
    }

    // We do not want to support the cloning-related operation for now.
    // They appear to mainly target corner-case scenarios in Windows itself,
    // and are (mainly) a historical artefact of abandoned early designs
    // for IRandomAccessStream. Cloning can be added in future, however, it
    // would be quite complex to support it correctly for generic streams.

    /// <inheritdoc cref="IRandomAccessStream.GetInputStreamAt"/>
    [SuppressMessage("Style", "IDE0060", Justification = "The 'position' parameter is part of the interface method signature.")]
    public IInputStream GetInputStreamAt(ulong position)
    {
        throw NotSupportedException.GetCloningNotSupportedException("GetInputStreamAt");
    }

    /// <inheritdoc cref="IRandomAccessStream.GetOutputStreamAt"/>
    [SuppressMessage("Style", "IDE0060", Justification = "The 'position' parameter is part of the interface method signature.")]
    public IOutputStream GetOutputStreamAt(ulong position)
    {
        throw NotSupportedException.GetCloningNotSupportedException("GetOutputStreamAt");
    }

    /// <inheritdoc cref="IRandomAccessStream.Seek"/>
    public void Seek(ulong position)
    {
        ArgumentException.ThrowIfPositionExceedsInt64MaxValue(position);

        Stream managedStream = EnsureNotDisposed();

        Debug.Assert(managedStream.CanSeek);
        Debug.Assert(position <= long.MaxValue);

        _ = managedStream.Seek(unchecked((long)position), SeekOrigin.Begin);
    }

    /// <inheritdoc cref="IRandomAccessStream.CloneStream"/>
    public IRandomAccessStream CloneStream()
    {
        throw NotSupportedException.GetCloningNotSupportedException("CloneStream");
    }
}