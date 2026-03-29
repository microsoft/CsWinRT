// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Windows.Storage.Streams;

#pragma warning disable IDE0270

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An adapter to wrap a managed <see cref="Stream"/> object and expose it as a native Windows Runtime stream.
/// </summary>
internal abstract partial class WindowsRuntimeNativeStreamAdapter : IDisposable
{
    /// <summary>
    /// The <see cref="StreamReadOperationOptimization"/> value to use for <see cref="_managedStream"/>.
    /// </summary>
    private readonly StreamReadOperationOptimization _readOptimization;

    /// <summary>
    /// The wrapped <see cref="Stream"/> instance.
    /// </summary>
    private Stream? _managedStream;

    /// <summary>
    /// Indicates whether to dispose <see cref="_managedStream"/> when <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    private bool _disposeManagedStream;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeNativeStreamAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> instance to wrap.</param>
    /// <param name="readOptimization">The <see cref="StreamReadOperationOptimization"/> value to use.</param>
    private WindowsRuntimeNativeStreamAdapter(Stream stream, StreamReadOperationOptimization readOptimization)
    {
        Debug.Assert(stream != null);
        Debug.Assert(stream.CanRead || stream.CanWrite || stream.CanSeek);
        Debug.Assert(!stream.CanRead || (stream.CanRead && this is IInputStream));
        Debug.Assert(!stream.CanWrite || (stream.CanWrite && this is IOutputStream));
        Debug.Assert(!stream.CanSeek || (stream.CanSeek && this is IRandomAccessStream));

        _readOptimization = readOptimization;
        _managedStream = stream;
    }

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeNativeStreamAdapter"/> instance specialized for the a given <see cref="Stream"/> object.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> instance to wrap.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeNativeStreamAdapter"/> instance wrapping <paramref name="stream"/>.</returns>
    public static WindowsRuntimeNativeStreamAdapter Create(Stream stream)
    {
        Debug.Assert(stream is not null);

        StreamReadOperationOptimization readOptimization = StreamReadOperationOptimization.Determine(stream);

        // Depending on the capabilities of the .NET 'Stream' object for which we need to construct the adapter, we
        // need to return an object that implements a well-known set of Windows Runtime interfaces (so that those
        // interfaces will be in the set of COM interface entries for the CCW of that object). E.g. if the specified
        // stream object reports 'CanRead', but not 'CanSeek' and not 'CanWrite', then we must return an object that
        // implements 'IInputStream', but not 'IRandomAccessStream' and not 'IOutputStream'. So we just use different
        // derived types implementing the various combinations of interfaces, and rely on 'cswinrtinteropgen' to produce
        // all necessary marshalling code for when instances of these types are passed to native callers as CCWs.
        return stream switch
        {
            { CanSeek: true } => new RandomAccessStream(stream, readOptimization),
            { CanRead: true, CanWrite: true } => new InputOutputStream(stream, readOptimization),
            { CanRead: true } => new InputStream(stream, readOptimization),
            { CanWrite: true } => new OutputStream(stream, readOptimization),
            _ => throw ArgumentException.GetNotSufficientCapabilitiesToConvertToWinRtStreamException()
        };
    }

    /// <summary>
    /// Marks the current instance has having been fully initialized, and takes ownership of disposal for the underlying managed stream.
    /// </summary>
    /// <remarks>
    /// We keep tables for mappings between managed and Windows Runtime streams to make sure to always return the same adapter for a given
    /// underlying stream. However, in order to avoid global locks on those tables, several instances of this type may be created and then
    /// can race to be entered into the appropriate map table. All except for the winning instances will be thrown away. However, we must
    /// ensure that when the losers are disposed, they do not dispose the underlying stream. To ensure that this is the case, we must call
    /// this method on the winner to notify it that it is safe to dispose the underlying stream.
    /// </remarks>
    public void SetWonInitializationRace()
    {
        _disposeManagedStream = true;
    }

    /// <summary>
    /// Gets the underlying managed stream, if the current instance has not been disposed.
    /// </summary>
    /// <returns>The underlying managed stream, if available.</returns>
    public Stream? GetManagedStream()
    {
        return _managedStream;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stream? managedStream = _managedStream;

        if (managedStream is null)
        {
            return;
        }

        _managedStream = null;

        if (_disposeManagedStream)
        {
            managedStream.Dispose();
        }
    }

    /// <summary>
    /// Ensures that the current instance has not been disposed and returns a valid stream instance.
    /// </summary>
    /// <returns>The underlying managed stream if the current instance has not been disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the current instance has been disposed.</exception>
    [MemberNotNull(nameof(_managedStream))]
    private Stream EnsureNotDisposed()
    {
        Stream? managedStream = _managedStream;

        if (managedStream is null)
        {
            throw ObjectDisposedException.GetStreamIsClosedException();
        }

        // This method throws if the stream is 'null', meaning it will only ever return if
        // '_managedStream' is not 'null'. Roslyn's flow analysis can't properly follow this
        // logic, so here we're manually suppressing this warning at the end of the method.
#pragma warning disable CS8774
        return managedStream;
#pragma warning restore CS8774
    }
}
#endif
