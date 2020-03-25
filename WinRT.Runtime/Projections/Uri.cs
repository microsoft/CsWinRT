
using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Windows.Foundation
{
    [Guid("9E365E57-48B2-4160-956F-C7385120BBFC")]
    internal struct IUriRuntimeClassVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        public _get_PropertyAsString get_AbsoluteUri_0;
        public _get_PropertyAsString get_DisplayUri_1;
        public _get_PropertyAsString get_Domain_2;
        public _get_PropertyAsString get_Extension_3;
        public _get_PropertyAsString get_Fragment_4;
        public _get_PropertyAsString get_Host_5;
        public _get_PropertyAsString get_Password_6;
        public _get_PropertyAsString get_Path_7;
        public _get_PropertyAsString get_Query_8;
        public IntPtr get_QueryParsed_9;
        public _get_PropertyAsString get_RawUri_10;
        public _get_PropertyAsString get_SchemeName_11;
        public _get_PropertyAsString get_UserName_12;
        public _get_PropertyAsInt32 get_Port_13;
        public _get_PropertyAsBoolean get_Suspicious_14;
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
        public struct Vftbl
        {
            public unsafe delegate int _CreateWithRelativeUri_1(IntPtr thisPtr, IntPtr baseUri, IntPtr relativeUri, out IntPtr instance);
            public unsafe delegate int _CreateUri_0(IntPtr thisPtr, IntPtr uri, out IntPtr instance);
            internal IInspectable.Vftbl IInspectableVftbl;
            public _CreateUri_0 CreateUri_0;
            public _CreateWithRelativeUri_1 CreateWithRelativeUri_1;
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

        public unsafe IObjectReference CreateWithRelativeUri(string baseUri, string relativeUri)
        {
            MarshalString __baseUri = default;
            MarshalString __relativeUri = default;
            IntPtr __retval = default;
            try
            {
                __baseUri = MarshalString.CreateMarshaler(baseUri);
                __relativeUri = MarshalString.CreateMarshaler(relativeUri);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateWithRelativeUri_1(ThisPtr, MarshalString.GetAbi(__baseUri), MarshalString.GetAbi(__relativeUri), out __retval));
                return ObjectReference<IUnknownVftbl>.Attach(ref __retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__baseUri);
                MarshalString.DisposeMarshaler(__relativeUri);
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Uri
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

        public static IntPtr GetAbi(IObjectReference m) => m.ThisPtr;

        public static global::System.Uri FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            using var uri = ObjectReference<ABI.Windows.Foundation.IUriRuntimeClassVftbl>.FromAbi(ptr);
            IntPtr rawUri = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(uri.Vftbl.get_RawUri_10(uri.ThisPtr, out rawUri));
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

        public static void DisposeMarshaler(IObjectReference m) { m.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref abi); }

        public static string GetGuidSignature()
        {
            return "rc(Windows.Foundation.Uri;{9E365E57-48B2-4160-956F-C7385120BBFC})";
        }
    }
}
