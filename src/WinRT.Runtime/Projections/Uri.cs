// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_RawUri_10 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_RawUri_10;
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
#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
#endif
    [Guid("44A9796F-723E-4FDF-A218-033E75B0C084")]
    internal sealed class WinRTUriRuntimeClassFactory
    {
        [Guid("44A9796F-723E-4FDF-A218-033E75B0C084")]
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _CreateUri_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int> CreateUri_0 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)_CreateUri_0;
            public IntPtr _CreateWithRelativeUri;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, IID.IID_UriRuntimeClassFactory);

        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public WinRTUriRuntimeClassFactory(IObjectReference obj) : this(obj.As<Vftbl>(IID.IID_UriRuntimeClassFactory)) { }
        public WinRTUriRuntimeClassFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IObjectReference CreateUri(string uri)
        {
            IntPtr __retval = default;
            MarshalString.Pinnable __uri = new(uri);
            fixed (void* ___uri = __uri)
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, MarshalString.GetAbi(ref __uri), &__retval));
                return ObjectReference<IUnknownVftbl>.Attach(ref __retval, IID.IID_IUnknown);
            }
        }

        public unsafe ObjectReferenceValue CreateUriForMarshaling(string uri)
        {
            IntPtr __retval = default;
            MarshalString.Pinnable __uri = new(uri);
            fixed (void* ___uri = __uri)
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, MarshalString.GetAbi(ref __uri), &__retval));
                return new ObjectReferenceValue(__retval);
            }
        }
    }


    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    unsafe struct Uri
    {
        private static readonly WinRTUriRuntimeClassFactory Instance = new(ActivationFactory.Get("Windows.Foundation.Uri"));

        public static IObjectReference CreateMarshaler(global::System.Uri value)
        {
            if (value is null)
            {
                return null;
            }

            return Instance.CreateUri(value.OriginalString);
        }

        public static ObjectReferenceValue CreateMarshaler2(global::System.Uri value)
        {
            if (value is null)
            {
                return new ObjectReferenceValue();
            }

            return Instance.CreateUriForMarshaling(value.OriginalString);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Uri FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            IntPtr rawUri = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR((**(ABI.Windows.Foundation.IUriRuntimeClassVftbl**)ptr).get_RawUri_10(ptr, &rawUri));
                return new global::System.Uri(MarshalString.FromAbi(rawUri));
            }
            finally
            {
                MarshalString.DisposeAbi(rawUri);
            }
        }

        public static unsafe global::System.Uri[] FromAbiArray(object box) => MarshalInterfaceHelper<global::System.Uri>.FromAbiArray(box, FromAbi);

        public static unsafe void CopyManaged(global::System.Uri o, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o).Detach();
        }

        public static IntPtr FromManaged(global::System.Uri value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler2(value).Detach();
        }

        public static unsafe MarshalInterfaceHelper<global::System.Uri>.MarshalerArray CreateMarshalerArray(global::System.Uri[] array) => MarshalInterfaceHelper<global::System.Uri>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.Uri>.GetAbiArray(box);
        public static void CopyAbiArray(global::System.Uri[] array, object box) => MarshalInterfaceHelper<global::System.Uri>.CopyAbiArray(array, box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::System.Uri[] array) => MarshalInterfaceHelper<global::System.Uri>.FromManagedArray(array, (o) => FromManaged(o));
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::System.Uri>.MarshalerArray array) => MarshalInterfaceHelper<global::System.Uri>.DisposeMarshalerArray(array);
        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { MarshalInspectable<object>.DisposeAbi(abi); }

        public static string GetGuidSignature()
        {
            return "rc(Windows.Foundation.Uri;{9e365e57-48b2-4160-956f-c7385120bbfc})";
        }
    }
}
