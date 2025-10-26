namespace ABI.Windows.Storage.Streams
{
    /// <summary>
    /// Binding type for the <c>IMarshal</c> interface vtable.
    /// </summary>
    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"/>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IBufferMarshalVftbl
    {
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void*, uint, void*, uint, Guid*, int> GetUnmarshalClass;
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void*, uint, void*, uint, uint*, int> GetMarshalSizeMax;
        public delegate* unmanaged[MemberFunction]<void*, void*, Guid*, void*, uint, void*, uint, int> MarshalInterface;
        public delegate* unmanaged[MemberFunction]<void*, void*, Guid*, void**, int> UnmarshalInterface;
        public delegate* unmanaged[MemberFunction]<void*, void*, int> ReleaseMarshalData;
        public delegate* unmanaged[MemberFunction]<void*, uint, int> DisconnectObject;

        /// <summary>
        /// Retrieves the CLSID of the unmarshaling code.
        /// </summary>
        /// <param name="thisPtr">The target COM object.</param>
        /// <param name="riid">A reference to the identifier of the interface to be marshaled.</param>
        /// <param name="pv">A pointer to the interface to be marshaled (can be <see langword="null"/>).</param>
        /// <param name="dwDestContext">The destination context where the specified interface is to be unmarshaled.</param>
        /// <param name="pvDestContext">This parameter is reserved and must be <see langword="null"/>.</param>
        /// <param name="mshlflags">Indicates whether the data to be marshaled is to be transmitted back to the client process or written to a global table.</param>
        /// <param name="pCid">A pointer that receives the CLSID to be used to create a proxy in the client process.</param>
        /// <returns>The <c>HRESULT</c> for the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUnmarshalClassUnsafe(
            void* thisPtr,
            Guid* riid,
            void* pv,
            uint dwDestContext,
            void* pvDestContext,
            uint mshlflags,
            Guid* pCid)
        {
            return ((IBufferMarshalVftbl*)*(void***)thisPtr)->GetUnmarshalClass(thisPtr, riid, pv, dwDestContext, pvDestContext, mshlflags, pCid);
        }

        /// <summary>
        /// Retrieves the maximum size of the buffer that will be needed during marshaling.
        /// </summary>
        /// <param name="thisPtr">The target COM object.</param>
        /// <param name="riid">A reference to the identifier of the interface to be marshaled.</param>
        /// <param name="pv">A pointer to the interface to be marshaled (can be <see langword="null"/>).</param>
        /// <param name="dwDestContext">The destination context where the specified interface is to be unmarshaled.</param>
        /// <param name="pvDestContext">This parameter is reserved and must be <see langword="null"/>.</param>
        /// <param name="mshlflags">Indicates whether the data to be marshaled is to be transmitted back to the client process or written to a global table.</param>
        /// <param name="pSize">A pointer to a variable that receives the maximum size of the buffer.</param>
        /// <returns>The <c>HRESULT</c> for the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMarshalSizeMaxUnsafe(
            void* thisPtr,
            Guid* riid,
            void* pv,
            uint dwDestContext,
            void* pvDestContext,
            uint mshlflags,
            uint* pSize)
        {
            return ((IBufferMarshalVftbl*)*(void***)thisPtr)->GetMarshalSizeMax(thisPtr, riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
        }

        /// <summary>
        /// Marshals an interface pointer.
        /// </summary>
        /// <param name="thisPtr">The target COM object.</param>
        /// <param name="pStm">A pointer to the stream to be used during marshaling.</param>
        /// <param name="riid">A reference to the identifier of the interface to be marshaled.</param>
        /// <param name="pv">A pointer to the interface to be marshaled (can be <see langword="null"/>).</param>
        /// <param name="dwDestContext">The destination context where the specified interface is to be unmarshaled.</param>
        /// <param name="pvDestContext">This parameter is reserved and must be <see langword="null"/>.</param>
        /// <param name="mshlflags">Indicates whether the data to be marshaled is to be transmitted back to the client process or written to a global table.</param>
        /// <returns>The <c>HRESULT</c> for the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MarshalInterfaceUnsafe(
            void* thisPtr,
            void* pStm,
            Guid* riid,
            void* pv,
            uint dwDestContext,
            void* pvDestContext,
            uint mshlflags)
        {
            return ((IBufferMarshalVftbl*)*(void***)thisPtr)->MarshalInterface(thisPtr, pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
        }

        /// <summary>
        /// Unmarshals an interface pointer.
        /// </summary>
        /// <param name="thisPtr">The target COM object.</param>
        /// <param name="pStm">A pointer to the stream from which the interface pointer is to be unmarshaled.</param>
        /// <param name="riid">A reference to the identifier of the interface to be unmarshaled.</param>
        /// <param name="ppv">The address of pointer variable that receives the interface pointer.</param>
        /// <returns>The <c>HRESULT</c> for the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UnmarshalInterfaceUnsafe(
            void* thisPtr,
            void* pStm,
            Guid* riid,
            void** ppv)
        {
            return ((IBufferMarshalVftbl*)*(void***)thisPtr)->UnmarshalInterface(thisPtr, pStm, riid, ppv);
        }

        /// <summary>
        /// Destroys a marshaled data packet.
        /// </summary>
        /// <param name="thisPtr">The target COM object.</param>
        /// <param name="pStm">A pointer to a stream that contains the data packet to be destroyed.</param>
        /// <returns>The <c>HRESULT</c> for the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReleaseMarshalDataUnsafe(void* thisPtr, void* pStm)
        {
            return ((IBufferMarshalVftbl*)*(void***)thisPtr)->ReleaseMarshalData(thisPtr, pStm);
        }

        /// <summary>
        /// Releases all connections to an object.
        /// </summary>
        /// <param name="thisPtr">The target COM object.</param>
        /// <param name="dwReserved">This parameter is reserved and must be <c>0</c>.</param>
        /// <returns>The <c>HRESULT</c> for the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DisconnectObjectUnsafe(void* thisPtr, uint dwReserved)
        {
            return ((IBufferMarshalVftbl*)*(void***)thisPtr)->DisconnectObject(thisPtr, dwReserved);
        }
    }

    internal static unsafe class IBufferMarshalImpl
    {
        /// <summary>
        /// The <see cref="IBufferMarshalVftbl"/> value for the managed <c>IMarshal</c> implementation using RoGetBufferMarshaler as its implementation.
        /// </summary>
        [FixedAddressValueType]
        private static readonly IBufferMarshalVftbl Vftbl;

        [ThreadStatic]
        private static void* t_winRtMarshalProxy = null;

        /// <summary>
        /// Initializes <see cref="Vftbl"/>.
        /// </summary>
        static IBufferMarshalImpl()
        {
            *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

            Vftbl.GetUnmarshalClass = &GetUnmarshalClass;
            Vftbl.GetMarshalSizeMax = &GetMarshalSizeMax;
            Vftbl.MarshalInterface = &MarshalInterface;
            Vftbl.UnmarshalInterface = &UnmarshalInterface;
            Vftbl.ReleaseMarshalData = &ReleaseMarshalData;
            Vftbl.DisconnectObject = &DisconnectObject;
        }

        [DllImport("api-ms-win-core-winrt-robuffer-l1-1-0.dll")]
        private static extern unsafe int RoGetBufferMarshaler(void** bufferMarshalerPtr);

        private const string WinTypesDLL = "WinTypes.dll";

        private static unsafe void EnsureHasMarshalProxy()
        {
            if (t_winRtMarshalProxy != null)
                return;

            try
            {
                void* proxyPtr = null;
                RestrictedErrorInfo.ThrowExceptionForHR(RoGetBufferMarshaler(&proxyPtr));
                if (proxyPtr == null)
                {
                    throw new NullReferenceException(string.Format("{0} ({1}!RoGetBufferMarshaler)", global::Windows.Storage.Streams.SR.WinRtCOM_Error, WinTypesDLL));
                }
                t_winRtMarshalProxy = proxyPtr;
            }
            catch (DllNotFoundException ex)
            {
                throw new NotImplementedException(string.Format(global::Windows.Storage.Streams.SR.NotImplemented_NativeRoutineNotFound,
                                                               string.Format("{0}!RoGetBufferMarshaler", WinTypesDLL)),
                                                  ex);
            }
        }

        /// <summary>
        /// Gets a pointer to the managed <c>IMarshal</c> implementation using RoGetBufferMarshaler.
        /// </summary>
        public static nint Vtable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (nint)Unsafe.AsPointer(in Vftbl);
        }

        /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getunmarshalclass"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetUnmarshalClass(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags, Guid* pCid)
        {
            *pCid = default;

            try
            {
                EnsureHasMarshalProxy();
                return IBufferMarshalVftbl.GetUnmarshalClassUnsafe(t_winRtMarshalProxy, riid, pv, dwDestContext, pvDestContext, mshlflags, pCid);
            }
            catch (Exception ex)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
            }
        }

        /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getmarshalsizemax"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetMarshalSizeMax(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags, uint* pSize)
        {
            *pSize = 0;

            try
            {
                EnsureHasMarshalProxy();
                return IBufferMarshalVftbl.GetMarshalSizeMaxUnsafe(t_winRtMarshalProxy, riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
            }
            catch (Exception ex)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
            }
        }

        /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-marshalinterface"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int MarshalInterface(void* thisPtr, void* pStm, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags)
        {
            try
            {
                EnsureHasMarshalProxy();
                return IBufferMarshalVftbl.MarshalInterfaceUnsafe(t_winRtMarshalProxy, pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
            }
            catch (Exception ex)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
            }
        }

        /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-unmarshalinterface"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int UnmarshalInterface(void* thisPtr, void* pStm, Guid* riid, void** ppv)
        {
            *ppv = null;

            try
            {
                EnsureHasMarshalProxy();
                return IBufferMarshalVftbl.UnmarshalInterfaceUnsafe(t_winRtMarshalProxy, pStm, riid, ppv);
            }
            catch (Exception ex)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
            }
        }

        /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-releasemarshaldata"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int ReleaseMarshalData(void* thisPtr, void* pStm)
        {
            try
            {
                EnsureHasMarshalProxy();
                return IBufferMarshalVftbl.ReleaseMarshalDataUnsafe(t_winRtMarshalProxy, pStm);
            }
            catch (Exception ex)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
            }
        }

        /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-disconnectobject"/>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int DisconnectObject(void* thisPtr, uint dwReserved)
        {
            try
            {
                EnsureHasMarshalProxy();
                return IBufferMarshalVftbl.DisconnectObjectUnsafe(t_winRtMarshalProxy, dwReserved);
            }
            catch (Exception ex)
            {
                return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
            }
        }
    }
}