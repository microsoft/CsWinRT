
using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Windows.Foundation
{
    [Guid("9E365E57-48B2-4160-956F-C7385120BBFC")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IUriRuntimeClassVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        public IntPtr get_AbsoluteUri_0;
        public IntPtr get_DisplayUri_1;
        public IntPtr get_Domain_2;
        public IntPtr get_Extension_3;
        public IntPtr get_Fragment_4;
        public IntPtr get_Host_5;
        public IntPtr get_Password_6;
        public IntPtr get_Path_7;
        public IntPtr get_Query_8;
        public IntPtr get_QueryParsed_9;
        public void* _get_RawUri_10;
        public delegate* stdcall<IntPtr, IntPtr*, int> get_RawUri_10 => (delegate* stdcall<IntPtr, IntPtr*, int>)_get_RawUri_10;
        public IntPtr get_SchemeName_11;
        public IntPtr get_UserName_12;
        public IntPtr get_Port_13;
        public IntPtr get_Suspicious_14;
        public IntPtr Equals_15;
        public IntPtr CombineUri_16;
    }
}

namespace ABI.System
{

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("44A9796F-723E-4FDF-A218-033E75B0C084")]
    internal class WinRTUriRuntimeClassFactory
    {
        [Guid("44A9796F-723E-4FDF-A218-033E75B0C084")]
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _CreateUri_0;
            public delegate* stdcall<IntPtr, IntPtr, out IntPtr, int> CreateUri_0 => (delegate* stdcall<IntPtr, IntPtr, out IntPtr, int>)_CreateUri_0;
            public IntPtr _CreateWithRelativeUri;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator WinRTUriRuntimeClassFactory(IObjectReference obj) => (obj != null) ? new WinRTUriRuntimeClassFactory(obj) : null;
        public static implicit operator WinRTUriRuntimeClassFactory(ObjectReference<Vftbl> obj) => (obj != null) ? new WinRTUriRuntimeClassFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public WinRTUriRuntimeClassFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public WinRTUriRuntimeClassFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IObjectReference CreateUri(string uri)
        {
            MarshalString __uri = default;
            IntPtr __retval = default;
            try
            {
                __uri = MarshalString.CreateMarshaler(uri);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateUri_0(ThisPtr, MarshalString.GetAbi(__uri), out __retval));
                return ObjectReference<IUnknownVftbl>.Attach(ref __retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__uri);
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Uri
    {
        private static WeakLazy<ActivationFactory> _uriActivationFactory = new WeakLazy<ActivationFactory>();

        private class ActivationFactory : BaseActivationFactory
        {
            public ActivationFactory() : base("Windows.Foundation", "Windows.Foundation.Uri")
            {
            }
        }

        public static IObjectReference CreateMarshaler(global::System.Uri value)
        {
            if (value is null)
            {
                return null;
            }

            WinRTUriRuntimeClassFactory factory = _uriActivationFactory.Value._As<WinRTUriRuntimeClassFactory.Vftbl>();
            return factory.CreateUri(value.OriginalString);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Uri FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            using var uri = ObjectReference<IUnknownVftbl>.FromAbi(ptr).As<ABI.Windows.Foundation.IUriRuntimeClassVftbl>();
            IntPtr rawUri = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(uri.Vftbl.get_RawUri_10(uri.ThisPtr, &rawUri));
                return new global::System.Uri(MarshalString.FromAbi(rawUri));
            }
            finally
            {
                MarshalString.DisposeAbi(rawUri);
            }
        }

        public static unsafe void CopyManaged(global::System.Uri o, IntPtr dest)
        {
            using var objRef = CreateMarshaler(o);
            *(IntPtr*)dest.ToPointer() = objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static IntPtr FromManaged(global::System.Uri value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref abi); }

        public static string GetGuidSignature()
        {
            return "rc(Windows.Foundation.Uri;{9e365e57-48b2-4160-956f-c7385120bbfc})";
        }
    }
}
