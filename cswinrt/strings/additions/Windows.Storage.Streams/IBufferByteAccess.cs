namespace Windows.Storage.Streams
{
    using global::System;

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal interface IBufferByteAccess
    {
        IntPtr Buffer { get; }
    }
}

namespace ABI.Windows.Storage.Streams
{
    using global::System;
    using global::System.Runtime.InteropServices;
//TODO: Update netstandard version by adding objectreferenceattribute 
#if NETSTANDARD2_0

    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal class IBufferByteAccess : global::Windows.Storage.Streams.IBufferByteAccess
    {
        [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
        public struct Vftbl
        {
            public delegate int _get_Buffer_0(IntPtr thisPtr, out IntPtr buffer);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _get_Buffer_0 get_Buffer_0;


            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    get_Buffer_0 = Do_Abi_get_Buffer_0
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            public Vftbl(IntPtr ptr)
            {
                this = Marshal.PtrToStructure<Vftbl>(ptr);
            }

            private static int Do_Abi_get_Buffer_0(IntPtr thisPtr, out IntPtr buffer)
            {
                buffer = default;
                try
                {
                    buffer = ComWrappersSupport.FindObject<global::Windows.Storage.Streams.IBufferByteAccess>(thisPtr).Buffer;
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
                Marshal.ThrowExceptionForHR(_obj.Vftbl.get_Buffer_0(ThisPtr, out IntPtr buffer));
                return buffer;
            }
        }
    }
#else
    [DynamicInterfaceCastableImplementation]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal interface IBufferByteAccess : global::Windows.Storage.Streams.IBufferByteAccess
    {
        [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
        public struct Vftbl
        {
            public delegate int _get_Buffer_0(IntPtr thisPtr, out IntPtr buffer);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _get_Buffer_0 get_Buffer_0;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    get_Buffer_0 = Do_Abi_get_Buffer_0
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<IUnknownVftbl>() + sizeof(IntPtr) * 12);
                Marshal.StructureToPtr(AbiToProjectionVftable.IUnknownVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[3] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Buffer_0);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IUnknownVftbl = Marshal.PtrToStructure<IUnknownVftbl>(vftblPtr.Vftbl);
                get_Buffer_0 = Marshal.GetDelegateForFunctionPointer<_get_Buffer_0>(vftbl[3]);
            }

            private static int Do_Abi_get_Buffer_0(IntPtr thisPtr, out IntPtr buffer)
            {
                buffer = default;
                try
                {
                    buffer = ComWrappersSupport.FindObject<global::Windows.Storage.Streams.IBufferByteAccess>(thisPtr).Buffer;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        IntPtr global::Windows.Storage.Streams.IBufferByteAccess.Buffer
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Windows.Storage.Streams.IBufferByteAccess).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                Marshal.ThrowExceptionForHR(_obj.Vftbl.get_Buffer_0(ThisPtr, out IntPtr buffer));
                return buffer;
            }
        }
    }
#endif
}
