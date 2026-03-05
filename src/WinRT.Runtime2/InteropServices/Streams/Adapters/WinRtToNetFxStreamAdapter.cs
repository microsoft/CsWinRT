// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Storage.Streams;

#pragma warning disable IDE0270

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <code>Stream</code> used to wrap a Windows Runtime stream to expose it as a managed steam.
/// </summary>
internal sealed partial class WinRtToNetFxStreamAdapter : Stream
{
    /// <summary>
    /// The Windows Runtime stream being wrapped
    /// </summary>
    private object? _windowsRuntimeStream;

    /// <summary>
    /// Indicates whether <see cref="_windowsRuntimeStream"/> is a readable stream.
    /// </summary>
    private readonly bool _canRead;

    /// <summary>
    /// Indicates whether <see cref="_windowsRuntimeStream"/> is a writeable stream.
    /// </summary>
    private readonly bool _canWrite;

    /// <summary>
    /// Indicates whether <see cref="_windowsRuntimeStream"/> is a stream supporting seeking.
    /// </summary>
    private readonly bool _canSeek;

    /// <summary>
    /// Indicates whether to dispose <see cref="_windowsRuntimeStream"/> when <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    private bool _disposeNativeStream;

    /// <summary>
    /// Creates a new <see cref="WinRtToNetFxStreamAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream instance to wrap.</param>
    /// <param name="canRead">Indicates whether <paramref name="windowsRuntimeStream"/> is a readable stream.</param>
    /// <param name="canWrite">Indicates whether <paramref name="windowsRuntimeStream"/> is a writeable stream.</param>
    /// <param name="canSeek">Indicates whether <paramref name="windowsRuntimeStream"/> is a stream supporting seeking.</param>
    private WinRtToNetFxStreamAdapter(object windowsRuntimeStream, bool canRead, bool canWrite, bool canSeek)
    {
        Debug.Assert(windowsRuntimeStream is not null);
        Debug.Assert(windowsRuntimeStream is IInputStream or IOutputStream or IRandomAccessStream);
        Debug.Assert(canSeek == (windowsRuntimeStream is IRandomAccessStream));

        // If a stream is readable, it must be an 'IInputStream', or if not it must either not be one, or explicitly have 'CanRead' be 'false'
        Debug.Assert((canRead && (windowsRuntimeStream is IInputStream)) ||
                     (!canRead && (windowsRuntimeStream is not IInputStream or IRandomAccessStream { CanRead: false })));

        // If a stream is writeable, it must be an 'IOutputStream', or if not it must either not be one, or explicitly have 'CanWrite' be 'false'
        Debug.Assert((canWrite && (windowsRuntimeStream is IOutputStream)) ||
                     (!canWrite && (windowsRuntimeStream is not IOutputStream or IRandomAccessStream { CanWrite: false })));

        _windowsRuntimeStream = windowsRuntimeStream;
        _canRead = canRead;
        _canWrite = canWrite;
        _canSeek = canSeek;
    }

    /// <summary>
    /// Creates a new <see cref="WinRtToNetFxStreamAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream instance to wrap.</param>
    /// <remarks>
    /// The <paramref name="windowsRuntimeStream"/> object must implement at least one of the following Windows Runtime
    /// interfaces: <see cref="IInputStream"/>, <see cref="IOutputStream"/>, or <see cref="IRandomAccessStream"/>.
    /// </remarks>
    public static WinRtToNetFxStreamAdapter Create(object windowsRuntimeStream)
    {
        Debug.Assert(windowsRuntimeStream is not null);

        bool canRead = windowsRuntimeStream is IInputStream;
        bool canWrite = windowsRuntimeStream is IOutputStream;
        bool canSeek = windowsRuntimeStream is IRandomAccessStream;

        // If we can't perform any operations on the input stream, then it's invalid
        if (!canRead && !canWrite && !canSeek)
        {
            throw ArgumentException.GetObjectMustBeWinRtStreamException();
        }

        // Proactively guard against a non-conforming implementations
        if (canSeek)
        {
            IRandomAccessStream randomAccessStream = (IRandomAccessStream)windowsRuntimeStream;

            if (!canRead && randomAccessStream.CanRead)
            {
                throw ArgumentException.GetCanReadStreamMustImplementIInputStreamException();
            }

            if (!canWrite && randomAccessStream.CanWrite)
            {
                throw ArgumentException.GetCanWriteStreamMustImplementIOutputStreamException();
            }

            // If we have an 'IRandomAccessStream' instance, its 'CanRead' property takes precedence here.
            // This is because the stream would also implement 'IInputStream' (because it's a base interface
            // of 'IRandomAccessStream'), but it doesn't mean it can actually be read from in this case.
            if (!randomAccessStream.CanRead)
            {
                canRead = false;
            }

            if (!randomAccessStream.CanWrite)
            {
                canWrite = false;
            }
        }

        // Check again that we can perform some useful operations. We repeat this check here
        // in case we have an 'IRandomAccessStream' that specifies it can't do any of these.
        if (!canRead && !canWrite)
        {
            throw ArgumentException.GetWinRtStreamCannotReadOrWriteException();
        }

        // Create the managed wrapper implementation around the input Windows Runtime stream
        return new WinRtToNetFxStreamAdapter(windowsRuntimeStream, canRead, canWrite, canSeek);
    }

    /// <inheritdoc cref="NetFxToWinRtStreamAdapter.SetWonInitializationRace"/>
    public void SetWonInitializationRace()
    {
        _disposeNativeStream = true;
    }

    /// <summary>
    /// Gets the underlying Windows Runtime stream, if the current instance has not been disposed.
    /// </summary>
    /// <returns>The underlying Windows Runtime stream, if available.</returns>
    public object? GetWindowsRuntimeStream()
    {
        return _windowsRuntimeStream;
    }

    /// <summary>
    /// Ensures that the current instance has not been disposed and returns a valid stream instance.
    /// </summary>
    /// <returns>The underlying Windows Runtime stream if the current instance has not been disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the current instance has been disposed.</exception>
    [MemberNotNull(nameof(_windowsRuntimeStream))]
    private object EnsureNotDisposed()
    {
        object? windowsRuntimeStream = _windowsRuntimeStream;

        if (windowsRuntimeStream is null)
        {
            throw ObjectDisposedException.GetStreamIsDisposedException();
        }

        // Same suppression as in 'NetFxToWinRtStreamAdapter.EnsureNotDisposed'
#pragma warning disable CS8774
        return windowsRuntimeStream;
#pragma warning restore CS8774
    }
}