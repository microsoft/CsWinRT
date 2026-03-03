// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <code>wrapper</code> for a managed stream that implements all WinRT stream operations.
/// This class must not implement any WinRT stream interfaces directly.
/// We never create instances of this class directly; instead we use classes defined in
/// the region Interface adapters to implement WinRT ifaces and create instances of those types.
/// See comment in that region for technical details.
/// </summary>
internal abstract partial class NetFxToWinRtStreamAdapter : IDisposable
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
    /// Creates a new <see cref="NetFxToWinRtStreamAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> instance to wrap.</param>
    /// <param name="readOptimization">The <see cref="StreamReadOperationOptimization"/> value to use.</param>
    private NetFxToWinRtStreamAdapter(Stream stream, StreamReadOperationOptimization readOptimization)
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
    /// Creates a new <see cref="NetFxToWinRtStreamAdapter"/> instance specialized for the a given <see cref="Stream"/> object.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> instance to wrap.</param>
    /// <returns>The resulting <see cref="NetFxToWinRtStreamAdapter"/> instance wrapping <paramref name="stream"/>.</returns>
    public static NetFxToWinRtStreamAdapter Create(Stream stream)
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
            _ => throw new ArgumentException(SR.Argument_NotSufficientCapabilitiesToConvertToWinRtStream)
        };
    }

    #region Tools and Helpers

    /// <summary>
    /// We keep tables for mappings between managed and WinRT streams to make sure to always return the same adapter for a given underlying stream.
    /// However, in order to avoid global locks on those tables, several instances of this type may be created and then can race to be entered
    /// into the appropriate map table. All except for the winning instances will be thrown away. However, we must ensure that when the losers are
    /// finalized, they do not dispose the underlying stream. To ensure that, we must call this method on the winner to notify it that it is safe to
    /// dispose the underlying stream.
    /// </summary>
    public void SetWonInitializationRace()
    {
        _disposeManagedStream = true;
    }

    public Stream? GetManagedStream()
    {
        return _managedStream;
    }

    [MemberNotNull(nameof(_managedStream))]
    private Stream EnsureNotDisposed()
    {
        Stream? managedStream = _managedStream;

        if (managedStream is null)
        {
            ObjectDisposedException ex = new ObjectDisposedException(SR.ObjectDisposed_CannotPerformOperation);
            ex.HResult = WellKnownErrorCodes.RO_E_CLOSED;
            throw ex;
        }

        // This method throws if the stream is 'null', meaning it will only ever return if
        // '_managedStream' is not 'null'. Roslyn's flow analysis can't properly follow this
        // logic, so here we're manually suppressing this warning at the end of the method.
#pragma warning disable CS8774
        return managedStream;
#pragma warning restore CS8774
    }

    #endregion Tools and Helpers

    #region Common public interface

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

    #endregion Common public interface

    #region IInputStream public interface

    /// <inheritdoc cref="IInputStream.ReadAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (count is < 0 or > int.MaxValue)
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(count));
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        if (buffer.Capacity < count)
        {
            ArgumentException ex = new ArgumentException(SR.Argument_InsufficientBufferCapacity);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        if (!(options == InputStreamOptions.None || options == InputStreamOptions.Partial || options == InputStreamOptions.ReadAhead))
        {
            ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        Stream managedStream = EnsureNotDisposed();

        // We can use this pattern to add more optimization options if necessary:
        //
        // StreamReadOperationOptimization.XxxxStream => StreamOperationsImplementation.Xxxx,ReadAsync(managedStream, buffer, count, options);
        return _readOptimization switch
        {
            StreamReadOperationOptimization.MemoryStream => StreamOperationsImplementation.MemoryStream.ReadAsync(managedStream, buffer, count),
            StreamReadOperationOptimization.AbstractStream => StreamOperationsImplementation.ReadAsync(managedStream, buffer, count, options),
            _ => throw new NotSupportedException(SR.NotSupported_UnrecognizedStreamReadOptimization)
        };
    }

    #endregion IInputStream public interface


    #region IOutputStream public interface

    /// <inheritdoc cref="IOutputStream.WriteAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Capacity < buffer.Length)
        {
            ArgumentException ex = new ArgumentException(SR.Argument_BufferLengthExceedsCapacity);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        Stream managedStream = EnsureNotDisposed();

        return StreamOperationsImplementation.WriteAsync(managedStream, buffer);
    }

    /// <inheritdoc cref="IOutputStream.FlushAsync"/>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public IAsyncOperation<bool> FlushAsync()
    {
        Stream managedStream = EnsureNotDisposed();

        return StreamOperationsImplementation.FlushAsync(managedStream);
    }

    #endregion IOutputStream public interface


    #region IRandomAccessStream public interface


    #region IRandomAccessStream public interface: Not cloning related

    /// <inheritdoc cref="IRandomAccessStream.Seek"/>
    public void Seek(ulong position)
    {
        if (position > long.MaxValue)
        {
            ArgumentException ex = new ArgumentException(SR.IO_CannotSeekBeyondInt64MaxValue);
            ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
            throw ex;
        }

        Stream managedStream = EnsureNotDisposed();

        Debug.Assert(managedStream.CanSeek);
        Debug.Assert(position <= long.MaxValue);

        _ = managedStream.Seek(unchecked((long)position), SeekOrigin.Begin);
    }

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
            if (value > long.MaxValue)
            {
                ArgumentException ex = new ArgumentException(SR.IO_CannotSetSizeBeyondInt64MaxValue);
                ex.HResult = WellKnownErrorCodes.E_INVALIDARG;
                throw ex;
            }

            Stream managedStream = EnsureNotDisposed();

            if (!managedStream.CanWrite)
            {
                InvalidOperationException ex = new InvalidOperationException(SR.InvalidOperation_CannotSetStreamSizeCannotWrite);
                ex.HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL;
                throw ex;
            }

            Debug.Assert(managedStream.CanSeek);
            Debug.Assert(value <= long.MaxValue);

            managedStream.SetLength(unchecked((long)value));
        }
    }

    #endregion IRandomAccessStream public interface: Not cloning related


    #region IRandomAccessStream public interface: Cloning related

    // We do not want to support the cloning-related operation for now.
    // They appear to mainly target corner-case scenarios in Windows itself,
    // and are (mainly) a historical artefact of abandoned early designs
    // for IRandonAccessStream.
    // Cloning can be added in future, however, it would be quite complex
    // to support it correctly for generic streams.
    private static void ThrowCloningNotSupported(string methodName)
    {
        NotSupportedException nse = new NotSupportedException(string.Format(SR.NotSupported_CloningNotSupported, methodName));
        nse.HResult = WellKnownErrorCodes.E_NOTIMPL;
        throw nse;
    }


    public IRandomAccessStream CloneStream()
    {
        ThrowCloningNotSupported("CloneStream");
        return null;
    }


    public IInputStream GetInputStreamAt(ulong position)
    {
        ThrowCloningNotSupported("GetInputStreamAt");
        return null;
    }


    public IOutputStream GetOutputStreamAt(ulong position)
    {
        ThrowCloningNotSupported("GetOutputStreamAt");
        return null;
    }
    #endregion IRandomAccessStream public interface: Cloning related

    #endregion IRandomAccessStream public interface

}