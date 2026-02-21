#if !CSWINRT_REFERENCE_PROJECTION

#nullable enable

namespace ABI.Windows.System
{
    /// <summary>
    /// Binding type for <see cref="DispatcherQueueProxyHandler"/>.
    /// </summary>
    internal unsafe struct DispatcherQueueProxyHandlerVftbl
    {
        public delegate* unmanaged<DispatcherQueueProxyHandler*, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged<DispatcherQueueProxyHandler*, uint> AddRef;
        public delegate* unmanaged<DispatcherQueueProxyHandler*, uint> Release;
        public delegate* unmanaged<DispatcherQueueProxyHandler*, int> Invoke;
    }

    /// <summary>
    /// A custom <c>IDispatcherQueueHandler</c> object, that internally stores a captured <see cref="SendOrPostCallback"/> instance and the
    /// input captured state. This allows consumers to enqueue a state and a cached stateless delegate without any managed allocations.
    /// </summary>
    internal unsafe struct DispatcherQueueProxyHandler
    {
        /// <summary>
        /// The shared vtable pointer for <see cref="DispatcherQueueProxyHandler"/> instances.
        /// </summary>
        [FixedAddressValueType]
        private static readonly DispatcherQueueProxyHandlerVftbl Vftbl;

        /// <summary>
        /// Initializes <see cref="Vftbl"/>.
        /// </summary>
        static DispatcherQueueProxyHandler()
        {
            Vftbl.QueryInterface = &Impl.QueryInterface;
            Vftbl.AddRef = &Impl.AddRef;
            Vftbl.Release = &Impl.Release;
            Vftbl.Invoke = &Impl.Invoke;
        }

        /// <summary>
        /// Gets a pointer to the managed <see cref="DispatcherQueueProxyHandler"/> implementation.
        /// </summary>
        public static nint Vtable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (nint)Unsafe.AsPointer(in Vftbl);
        }

        /// <summary>
        /// The vtable pointer for the current instance.
        /// </summary>
        private nint vtbl;

        /// <summary>
        /// The <see cref="GCHandle"/> to the captured <see cref="global::System.Threading.SendOrPostCallback"/>.
        /// </summary>
        private GCHandle callbackHandle;

        /// <summary>
        /// The <see cref="GCHandle"/> to the captured state (if present, or a <see langword="null"/> handle otherwise).
        /// </summary>
        private GCHandle stateHandle;

        /// <summary>
        /// The current reference count for the object (from <c>IUnknown</c>).
        /// </summary>
        private volatile uint referenceCount;

        /// <summary>
        /// Creates a new <see cref="DispatcherQueueProxyHandler"/> instance for the input callback and state.
        /// </summary>
        /// <param name="handler">The input <see cref="global::System.Threading.SendOrPostCallback"/> callback to enqueue.</param>
        /// <param name="state">The input state to capture and pass to the callback.</param>
        /// <returns>A pointer to the newly initialized <see cref="DispatcherQueueProxyHandler"/> instance.</returns>
        public static DispatcherQueueProxyHandler* Create(global::System.Threading.SendOrPostCallback handler, object? state)
        {
            DispatcherQueueProxyHandler* @this = (DispatcherQueueProxyHandler*)NativeMemory.Alloc((nuint)sizeof(DispatcherQueueProxyHandler));

            @this->vtbl = Vtable;
            @this->callbackHandle = GCHandle.Alloc(handler);
            @this->stateHandle = state is not null ? GCHandle.Alloc(state) : default;
            @this->referenceCount = 1;

            return @this;
        }

        /// <summary>
        /// Devirtualized API for <c>IUnknown.Release()</c>.
        /// </summary>
        /// <returns>The updated reference count for the current instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Release()
        {
            uint referenceCount = global::System.Threading.Interlocked.Decrement(ref this.referenceCount);

            if (referenceCount == 0)
            {
                callbackHandle.Free();

                if (stateHandle.IsAllocated)
                {
                    stateHandle.Free();
                }

                NativeMemory.Free(Unsafe.AsPointer(ref this));
            }

            return referenceCount;
        }

        /// <summary>
        /// A private type with the implementation of the unmanaged methods for <see cref="DispatcherQueueProxyHandler"/>.
        /// These methods will be set into the shared vtable and invoked by WinRT from the object passed to it as an interface.
        /// </summary>
        private static class Impl
        {
            /// <summary>
            /// The HRESULT for a successful operation.
            /// </summary>
            private const int S_OK = 0;

            /// <summary>
            /// The HRESULT for an invalid cast from <c>IUnknown.QueryInterface</c>.
            /// </summary>
            private const int E_NOINTERFACE = unchecked((int)0x80004002);

            /// <summary>The IID for <c>IDispatcherQueueHandler</c> (2E0872A9-4E29-5F14-B688-FB96D5F9D5F8).</summary>
            private static ref readonly Guid IID_IDispatcherQueueHandler
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ReadOnlySpan<byte> data =
                    [
                        0xA9, 0x72, 0x08, 0x2E,
                        0x29, 0x4E,
                        0x14, 0x5F,
                        0xB6,
                        0x88,
                        0xFB,
                        0x96,
                        0xD5,
                        0xF9,
                        0xD5,
                        0xF8
                    ];

                    return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
                }
            }

            /// <summary>
            /// Implements <c>IUnknown.QueryInterface(REFIID, void**)</c>.
            /// </summary>
            [UnmanagedCallersOnly]
            public static int QueryInterface(DispatcherQueueProxyHandler* @this, Guid* riid, void** ppvObject)
            {
                if (riid->Equals(WellKnownInterfaceIIDs.IID_IUnknown) ||
                    riid->Equals(WellKnownInterfaceIIDs.IID_IAgileObject) ||
                    riid->Equals(IID_IDispatcherQueueHandler))
                {
                    global::System.Threading.Interlocked.Increment(ref @this->referenceCount);

                    *ppvObject = @this;

                    return S_OK;
                }

                return E_NOINTERFACE;
            }

            /// <summary>
            /// Implements <c>IUnknown.AddRef()</c>.
            /// </summary>
            [UnmanagedCallersOnly]
            public static uint AddRef(DispatcherQueueProxyHandler* @this)
            {
                return global::System.Threading.Interlocked.Increment(ref @this->referenceCount);
            }

            /// <summary>
            /// Implements <c>IUnknown.Release()</c>.
            /// </summary>
            [UnmanagedCallersOnly]
            public static uint Release(DispatcherQueueProxyHandler* @this)
            {
                uint referenceCount = global::System.Threading.Interlocked.Decrement(ref @this->referenceCount);

                if (referenceCount == 0)
                {
                    @this->callbackHandle.Free();

                    if (@this->stateHandle.IsAllocated)
                    {
                        @this->stateHandle.Free();
                    }

                    NativeMemory.Free(@this);
                }

                return referenceCount;
            }

            /// <summary>
            /// Implements <c>IDispatcherQueueHandler.Invoke()</c>.
            /// </summary>
            [UnmanagedCallersOnly]
            public static int Invoke(DispatcherQueueProxyHandler* @this)
            {
                object callback = @this->callbackHandle.Target!;
                object? state = @this->stateHandle.IsAllocated ? @this->stateHandle.Target! : null;

                try
                {
                    Unsafe.As<global::System.Threading.SendOrPostCallback>(callback)(state);
                }
                catch (Exception e)
                {
                    // This method triggers the special exception serialization logic in Native AOT that
                    // allows stacktraces from async crashes such as this to be correctly captured in crash dumps.
                    global::System.Runtime.ExceptionServices.ExceptionHandling.RaiseAppDomainUnhandledExceptionEvent(e);

                    // Register the exception with the global error handler. The failfast behavior
                    // is governed by the state of the 'UnhandledExceptionEventArgs.Handled' property.
                    // If 'Handled' is true the app continues running, else it failfasts.
                    RestrictedErrorInfo.ReportUnhandledError(e);
                }

                return S_OK;
            }
        }
    }
}

#nullable disable

#endif