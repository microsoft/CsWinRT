namespace Windows.Storage.Streams
{
    using global::System;

    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    internal interface IBufferByteAccess
    {
        IntPtr Buffer { get; }
    }
}

namespace ABI.Windows.Storage.Streams
{
    using global::System;
    using System.Runtime.InteropServices;

    [Guid("00000003-0000-0000-c000-000000000046")]
    internal class IBufferByteAccess : global::Windows.Storage.Streams.IBufferByteAccess
    {
        [Guid("00000003-0000-0000-c000-000000000046")]
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

            private static int Do_Abi_get_Buffer_0(IntPtr thisPtr, out IntPtr buffer)
            {
                try
                {
                    buffer = ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).Buffer;
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
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
}