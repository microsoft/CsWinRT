// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;

    /// <summary>
    /// Contains an implementation of the WinRT IBuffer interface that conforms to all requirements on classes that implement that interface,
    /// such as implementing additional interfaces.
    /// </summary>
    public sealed class WindowsRuntimeBuffer : IBuffer, IBufferByteAccess
    {
        #region Constants

        private const int E_BOUNDS = unchecked((int)0x8000000B);

        #endregion Constants


        #region Static factory methods

        public static IBuffer Create(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            return new WindowsRuntimeBuffer(capacity);
        }


        public static IBuffer Create(byte[] data, int offset, int length, int capacity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (data.Length - offset < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (data.Length - offset < capacity) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (capacity < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientBufferCapacity);

            byte[] underlyingData = new byte[capacity];
            Array.Copy(data, offset, underlyingData, 0, length);
            return new WindowsRuntimeBuffer(underlyingData, 0, length, capacity);
        }

        #endregion Static factory methods

        #region Fields

        private readonly byte[] _data;
        private readonly int _dataStartOffs = 0;
        private int _usefulDataLength = 0;
        private readonly int _maxDataCapacity = 0;
        private GCHandle _pinHandle;

        // Pointer to data[dataStartOffs] when data is pinned:
        private IntPtr _dataPtr = IntPtr.Zero;

        #endregion Fields


        #region Constructors

        internal WindowsRuntimeBuffer(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _data = new byte[capacity];
            _dataStartOffs = 0;
            _usefulDataLength = 0;
            _maxDataCapacity = capacity;
            _dataPtr = IntPtr.Zero;
        }


        internal WindowsRuntimeBuffer(byte[] data, int offset, int length, int capacity)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (data.Length - offset < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (data.Length - offset < capacity) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (capacity < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientBufferCapacity);

            _data = data;
            _dataStartOffs = offset;
            _usefulDataLength = length;
            _maxDataCapacity = capacity;
            _dataPtr = IntPtr.Zero;
        }

        #endregion Constructors


        #region Helpers

        internal void GetUnderlyingData(out byte[] underlyingDataArray, out int underlyingDataArrayStartOffset)
        {
            underlyingDataArray = _data;
            underlyingDataArrayStartOffset = _dataStartOffs;
        }

        private unsafe byte* PinUnderlyingData()
        {
            GCHandle gcHandle = default(GCHandle);
            bool ptrWasStored = false;
            IntPtr buffPtr;

            try { }
            finally
            {
                try
                {
                    // Pin the data array:
                    gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                    buffPtr = gcHandle.AddrOfPinnedObject() + _dataStartOffs;

                    // Store the pin IFF it has not been assigned:
                    ptrWasStored = (Interlocked.CompareExchange(ref _dataPtr, buffPtr, IntPtr.Zero) == IntPtr.Zero);
                }
                finally
                {
                    if (!ptrWasStored)
                    {
                        // There is a race with another thread also trying to create a pin and they were first
                        // in assigning to data pin. That's ok, just give it up.
                        // Unpin again (the pin from the other thread remains):
                        gcHandle.Free();
                    }
                    else
                    {
                        if (_pinHandle.IsAllocated)
                            _pinHandle.Free();

                        // Make sure we keep track of the handle
                        _pinHandle = gcHandle;
                    }
                }
            }

            // Ok, now all is good:
            return (byte*)buffPtr;
        }

        ~WindowsRuntimeBuffer()
        {
            if (_pinHandle.IsAllocated)
                _pinHandle.Free();
        }

        #endregion Helpers


        #region Implementation of Windows.Foundation.IBuffer

        uint IBuffer.Capacity
        {
            get { return unchecked((uint)_maxDataCapacity); }
        }


        uint IBuffer.Length
        {
            get
            {
                return unchecked((uint)_usefulDataLength);
            }

            set
            {
                if (value > ((IBuffer)this).Capacity)
                {
                    ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(nameof(value), global::Windows.Storage.Streams.SR.Argument_BufferLengthExceedsCapacity);
                    ex.HResult = E_BOUNDS;
                    throw ex;
                }

                // Capacity is ensured to not exceed Int32.MaxValue, so Length is within this limit and this cast is safe:
                Debug.Assert(((IBuffer)this).Capacity <= int.MaxValue);
                _usefulDataLength = unchecked((int)value);
            }
        }

        #endregion Implementation of Windows.Foundation.IBuffer


        #region Implementation of IBufferByteAccess

        unsafe byte* IBufferByteAccess.Buffer
        {
            get
            {
                // Get pin handle:
                IntPtr buffPtr = Volatile.Read(ref _dataPtr);

                // If we are already pinned, return the pointer:
                if (buffPtr != IntPtr.Zero)
                    return (byte*)buffPtr;

                // Otherwise pin it.
                return PinUnderlyingData();
            }
        }

        #endregion Implementation of IBufferByteAccess
    }  // class WindowsRuntimeBuffer
}  // namespace

namespace ABI.System.Runtime.InteropServices.WindowsRuntime
{
    [WindowsRuntimeClassName("Windows.Storage.Streams.IBuffer")]
    [WindowsRuntimeBufferComWrappersMarshaller]
    file static class WindowsRuntimeBuffer;

    file struct WindowsRuntimeBufferInterfaceEntries
    {
        public ComInterfaceEntry IBuffer;
        public ComInterfaceEntry IBufferByteAccess;
        public ComInterfaceEntry IStringable;
        public ComInterfaceEntry IWeakReferenceSource;
        public ComInterfaceEntry IMarshal;
        public ComInterfaceEntry IAgileObject;
        public ComInterfaceEntry IInspectable;
        public ComInterfaceEntry IUnknown;
    }

    file static class WindowsRuntimeBufferInterfaceEntriesImpl
    {
        [FixedAddressValueType]
        public static readonly WindowsRuntimeBufferInterfaceEntries Entries;

        /// <summary>
        /// Initializes <see cref="Entries"/>.
        /// </summary>
        static WindowsRuntimeBufferInterfaceEntriesImpl()
        {
            Entries.IBuffer.IID = ABI.InterfaceIIDs.IID_Windows_Storage_Streams_IBuffer;
            Entries.IBuffer.Vtable = ABI.Windows.Storage.Streams.IBufferImpl.Vtable;
            Entries.IBufferByteAccess.IID = ABI.Windows.Storage.Streams.WellKnownStreamInterfaceIIDs.IID_IBufferByteAccess;
            Entries.IBufferByteAccess.Vtable = ABI.Windows.Storage.Streams.IBufferByteAccessImpl.Vtable;
            Entries.IStringable.IID = IStringableImpl.IID;
            Entries.IStringable.Vtable = IStringableImpl.Vtable;
            Entries.IWeakReferenceSource.IID = IWeakReferenceSourceImpl.IID;
            Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
            Entries.IMarshal.IID = ABI.Windows.Storage.Streams.WellKnownStreamInterfaceIIDs.IID_IMarshal;
            Entries.IMarshal.Vtable = ABI.Windows.Storage.Streams.IBufferMarshalImpl.Vtable;
            Entries.IAgileObject.IID = IAgileObjectImpl.IID;
            Entries.IAgileObject.Vtable = IAgileObjectImpl.Vtable;
            Entries.IInspectable.IID = IInspectableImpl.IID;
            Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
            Entries.IUnknown.IID = IUnknownImpl.IID;
            Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
        }
    }

    file sealed unsafe class WindowsRuntimeBufferComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
    {
        /// <inheritdoc/>
        public override void* GetOrCreateComInterfaceForObject(object value)
        {
            return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
        }

        /// <inheritdoc/>
        public override ComInterfaceEntry* ComputeVtables(out int count)
        {
            count = sizeof(WindowsRuntimeBufferInterfaceEntries) / sizeof(ComInterfaceEntry);

            return (ComInterfaceEntry*)Unsafe.AsPointer(in WindowsRuntimeBufferInterfaceEntriesImpl.Entries);
        }
    }
}

// WindowsRuntimeBuffer.cs
