namespace Windows.Storage.Streams
{
    using global::System;

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.Storage.Streams.IBufferByteAccess))]
    internal interface IBufferByteAccess
    {
        IntPtr Buffer { get; }
    }
}

namespace ABI.Windows.Storage.Streams
{
    using global::System;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using global::System.ComponentModel;

#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal unsafe class IBufferByteAccess : global::Windows.Storage.Streams.IBufferByteAccess
    {
        [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _get_Buffer_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_Buffer_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_Buffer_0; set => _get_Buffer_0 = value; }

            public unsafe delegate int _get_Buffer_0_delegate(IntPtr thisPtr, IntPtr* result);
            private static readonly _get_Buffer_0_delegate DelegateCache;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    _get_Buffer_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache = Do_Abi_get_Buffer_0).ToPointer()
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            private static int Do_Abi_get_Buffer_0(IntPtr thisPtr, IntPtr* buffer)
            {
                *buffer = default;
                try
                {
                    *buffer = ComWrappersSupport.FindObject<global::Windows.Storage.Streams.IBufferByteAccess>(thisPtr).Buffer;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IBufferByteAccess(IObjectReference obj) => (obj != null) ? new IBufferByteAccess(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IBufferByteAccess(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IBufferByteAccess(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IntPtr Buffer
        {
            get
            {
                IntPtr __retval = default;
                Marshal.ThrowExceptionForHR(_obj.Vftbl.get_Buffer_0(ThisPtr, &__retval));
                return __retval;
            }
        }
    }
#else
    [DynamicInterfaceCastableImplementation]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal unsafe interface IBufferByteAccess : global::Windows.Storage.Streams.IBufferByteAccess
    {
        [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
        public struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> Get_Buffer_0;

            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.Interop.IUnknownVftbl>() + sizeof(IntPtr) * 1);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    Get_Buffer_0 = &Do_Abi_get_Buffer_0,
                };
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_get_Buffer_0(IntPtr thisPtr, IntPtr* buffer)
            {
                *buffer = default;
                try
                {
                    *buffer = ComWrappersSupport.FindObject<global::Windows.Storage.Streams.IBufferByteAccess>(thisPtr).Buffer;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_IBufferByteAccess);

        IntPtr global::Windows.Storage.Streams.IBufferByteAccess.Buffer
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Storage.Streams.IBufferByteAccess).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                IntPtr buffer = default;
                Marshal.ThrowExceptionForHR(_obj.Vftbl.Get_Buffer_0(ThisPtr, &buffer));
                return buffer;
            }
        }
    }
#endif
}
