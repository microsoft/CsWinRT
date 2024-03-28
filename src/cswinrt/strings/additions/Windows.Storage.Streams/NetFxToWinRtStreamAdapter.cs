// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.IO
{
    using System.Diagnostics;
    using System.IO;
    
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Threading;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;
    using System.Diagnostics.CodeAnalysis;
    /// <summary>
    /// An <code>wrapper</code> for a managed stream that implements all WinRT stream operations.
    /// This class must not implement any WinRT stream interfaces directly.
    /// We never create instances of this class directly; instead we use classes defined in
    /// the region Interface adapters to implement WinRT ifaces and create instances of those types.
    /// See comment in that region for technical details.
    /// </summary>
    internal abstract partial class NetFxToWinRtStreamAdapter : IDisposable
    {
        private const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);
        private const int RO_E_CLOSED = unchecked((int)0x80000013);
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const int E_INVALIDARG = unchecked((int)0x80070057);
        #region Construction

        #region Interface adapters

        // Instances of private types defined in this section will be returned from NetFxToWinRtStreamAdapter.Create(..).
        // Depending on the capabilities of the .NET stream for which we need to construct the adapter, we need to return
        // an object that can be QIed (COM speak for "cast") to a well-defined set of ifaces.
        // E.g, if the specified stream CanRead, but not CanSeek and not CanWrite, then we *must* return an object that
        // can be QIed to IInputStream, but *not* IRandomAccessStream and *not* IOutputStream.
        // There are two ways to do that:
        //   - We could explicitly implement ICustomQueryInterface and respond to QI requests by analyzing the stream capabilities
        //   - We can use the runtime's ability to do that for us, based on the ifaces the concrete class implements (or does not).
        // The latter is much more elegant, and likely also faster.


        private sealed partial class InputStream : NetFxToWinRtStreamAdapter, IInputStream, IDisposable
        {
            internal InputStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }


        private sealed partial class OutputStream : NetFxToWinRtStreamAdapter, IOutputStream, IDisposable
        {
            internal OutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }


        private sealed partial class RandomAccessStream : NetFxToWinRtStreamAdapter, IRandomAccessStream, IInputStream, IOutputStream, IDisposable
        {
            internal RandomAccessStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }


        private sealed partial class InputOutputStream : NetFxToWinRtStreamAdapter, IInputStream, IOutputStream, IDisposable
        {
            internal InputOutputStream(Stream stream, StreamReadOperationOptimization readOptimization)
                : base(stream, readOptimization)
            {
            }
        }

        #endregion Interface adapters

        // We may want to define different behaviour for different types of streams.
        // For instance, ReadAsync treats MemoryStream special for performance reasons.
        // The enum 'StreamReadOperationOptimization' describes the read optimization to employ for a
        // given NetFxToWinRtStreamAdapter instance. In future, we might define other enums to follow a
        // similar pattern, e.g. 'StreamWriteOperationOptimization' or 'StreamFlushOperationOptimization'.
        private enum StreamReadOperationOptimization
        {
            AbstractStream = 0, MemoryStream
        }


        internal static NetFxToWinRtStreamAdapter Create(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            StreamReadOperationOptimization readOptimization = StreamReadOperationOptimization.AbstractStream;
            if (stream.CanRead)
                readOptimization = DetermineStreamReadOptimization(stream);

            NetFxToWinRtStreamAdapter adapter;

            if (stream.CanSeek)
                adapter = new RandomAccessStream(stream, readOptimization);

            else if (stream.CanRead && stream.CanWrite)
                adapter = new InputOutputStream(stream, readOptimization);

            else if (stream.CanRead)
                adapter = new InputStream(stream, readOptimization);

            else if (stream.CanWrite)
                adapter = new OutputStream(stream, readOptimization);

            else
                throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_NotSufficientCapabilitiesToConvertToWinRtStream);

            return adapter;
        }


        private static StreamReadOperationOptimization DetermineStreamReadOptimization(Stream stream)
        {
            Debug.Assert(stream != null);

            if (CanApplyReadMemoryStreamOptimization(stream))
                return StreamReadOperationOptimization.MemoryStream;

            return StreamReadOperationOptimization.AbstractStream;
        }


        private static bool CanApplyReadMemoryStreamOptimization(Stream stream)
        {
            MemoryStream memStream = stream as MemoryStream;
            if (memStream == null)
                return false;

            ArraySegment<byte> arrSeg;
            return memStream.TryGetBuffer(out arrSeg);
        }


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

        #endregion Construction


        #region Instance variables

        private Stream _managedStream = null;
        private bool _leaveUnderlyingStreamOpen = true;
        private readonly StreamReadOperationOptimization _readOptimization;

        #endregion Instance variables


        #region Tools and Helpers

        /// <summary>
        /// We keep tables for mappings between managed and WinRT streams to make sure to always return the same adapter for a given underlying stream.
        /// However, in order to avoid global locks on those tables, several instances of this type may be created and then can race to be entered
        /// into the appropriate map table. All except for the winning instances will be thrown away. However, we must ensure that when the losers are
        /// finalized, they do not dispose the underlying stream. To ensure that, we must call this method on the winner to notify it that it is safe to
        /// dispose the underlying stream.
        /// </summary>
        internal void SetWonInitializationRace()
        {
            _leaveUnderlyingStreamOpen = false;
        }


        public Stream GetManagedStream()
        {
            return _managedStream;
        }


        private Stream EnsureNotDisposed()
        {
            Stream str = _managedStream;

            if (str == null)
            {
                ObjectDisposedException ex = new ObjectDisposedException(global::Windows.Storage.Streams.SR.ObjectDisposed_CannotPerformOperation);
                ex.SetHResult(RO_E_CLOSED);
                throw ex;
            }

            return str;
        }

        #endregion Tools and Helpers


        #region Common public interface

        /// <summary>Implements IDisposable.Dispose (IClosable.Close in WinRT)</summary>
        void IDisposable.Dispose()
        {
            Stream str = _managedStream;
            if (str == null)
                return;

            _managedStream = null;

            if (!_leaveUnderlyingStreamOpen)
                str.Dispose();
        }

        #endregion Common public interface


        #region IInputStream public interface

#if NET
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            if (buffer == null)
            {
                // Mapped to E_POINTER.
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count < 0 || int.MaxValue < count)
            {
                ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(count));
                ex.SetHResult(E_INVALIDARG);
                throw ex;
            }

            if (buffer.Capacity < count)
            {
                ArgumentException ex = new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientBufferCapacity);
                ex.SetHResult(E_INVALIDARG);
                throw ex;
            }

            if (!(options == InputStreamOptions.None || options == InputStreamOptions.Partial || options == InputStreamOptions.ReadAhead))
            {
                ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(options),
                                                                                 global::Windows.Storage.Streams.SR.ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue);
                ex.SetHResult(E_INVALIDARG);
                throw ex;
            }

            Stream str = EnsureNotDisposed();

            IAsyncOperationWithProgress<IBuffer, uint> readAsyncOperation;
            switch (_readOptimization)
            {
                case StreamReadOperationOptimization.MemoryStream:
                    readAsyncOperation = StreamOperationsImplementation.ReadAsync_MemoryStream(str, buffer, count);
                    break;

                case StreamReadOperationOptimization.AbstractStream:
                    readAsyncOperation = StreamOperationsImplementation.ReadAsync_AbstractStream(str, buffer, count, options);
                    break;

                // Use this pattern to add more optimisation options if necessary:
                //case StreamReadOperationOptimization.XxxxStream:
                //    readAsyncOperation = StreamOperationsImplementation.ReadAsync_XxxxStream(str, buffer, count, options);
                //    break;

                default:
                    Debug.Fail("We should never get here. Someone forgot to handle an input stream optimisation option.");
                    readAsyncOperation = null;
                    break;
            }

            return readAsyncOperation;
        }

        #endregion IInputStream public interface


        #region IOutputStream public interface

#if NET
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            if (buffer == null)
            {
                // Mapped to E_POINTER.
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Capacity < buffer.Length)
            {
                ArgumentException ex = new ArgumentException(global::Windows.Storage.Streams.SR.Argument_BufferLengthExceedsCapacity);
                ex.SetHResult(E_INVALIDARG);
                throw ex;
            }

            Stream str = EnsureNotDisposed();
            return StreamOperationsImplementation.WriteAsync_AbstractStream(str, buffer);
        }

#if NET
        [global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
        public IAsyncOperation<bool> FlushAsync()
        {
            Stream str = EnsureNotDisposed();
            return StreamOperationsImplementation.FlushAsync_AbstractStream(str);
        }

        #endregion IOutputStream public interface


        #region IRandomAccessStream public interface


        #region IRandomAccessStream public interface: Not cloning related

        public void Seek(ulong position)
        {
            if (position > long.MaxValue)
            {
                ArgumentException ex = new ArgumentException(global::Windows.Storage.Streams.SR.IO_CannotSeekBeyondInt64MaxValue);
                ex.SetHResult(E_INVALIDARG);
                throw ex;
            }

            Stream str = EnsureNotDisposed();
            long pos = unchecked((long)position);

            Debug.Assert(str != null);
            Debug.Assert(str.CanSeek, "The underlying str is expected to support Seek, but it does not.");
            Debug.Assert(0 <= pos, "Unexpected pos=" + pos + ".");

            str.Seek(pos, SeekOrigin.Begin);
        }


        public bool CanRead
        {
            get
            {
                Stream str = EnsureNotDisposed();
                return str.CanRead;
            }
        }


        public bool CanWrite
        {
            get
            {
                Stream str = EnsureNotDisposed();
                return str.CanWrite;
            }
        }


        public ulong Position
        {
            get
            {
                Stream str = EnsureNotDisposed();
                return (ulong)str.Position;
            }
        }


        public ulong Size
        {
            get
            {
                Stream str = EnsureNotDisposed();
                return (ulong)str.Length;
            }

            set
            {
                if (value > long.MaxValue)
                {
                    ArgumentException ex = new ArgumentException(global::Windows.Storage.Streams.SR.IO_CannotSetSizeBeyondInt64MaxValue);
                    ex.SetHResult(E_INVALIDARG);
                    throw ex;
                }

                Stream str = EnsureNotDisposed();

                if (!str.CanWrite)
                {
                    InvalidOperationException ex = new InvalidOperationException(global::Windows.Storage.Streams.SR.InvalidOperation_CannotSetStreamSizeCannotWrite);
                    ex.SetHResult(E_ILLEGAL_METHOD_CALL);
                    throw ex;
                }

                long val = unchecked((long)value);

                Debug.Assert(str != null);
                Debug.Assert(str.CanSeek, "The underlying str is expected to support Seek, but it does not.");
                Debug.Assert(0 <= val, "Unexpected val=" + val + ".");

                str.SetLength(val);
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
            NotSupportedException nse = new NotSupportedException(string.Format(global::Windows.Storage.Streams.SR.NotSupported_CloningNotSupported, methodName));
            nse.SetHResult(E_NOTIMPL);
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

    }  // class NetFxToWinRtStreamAdapter
}  // namespace

// NetFxToWinRtStreamAdapter.cs
