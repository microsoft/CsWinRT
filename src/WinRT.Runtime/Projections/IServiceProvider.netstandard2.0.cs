// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.System
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
#if EMBED
    internal
#else
    public
#endif
    unsafe class IServiceProvider : global::System.IServiceProvider
    {
        [Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            private void* _GetService_0;
            public delegate* unmanaged[Stdcall]<IntPtr, global::ABI.System.Type, IntPtr*, int> GetService_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, global::ABI.System.Type, IntPtr*, int>)_GetService_0; set => _GetService_0 = (void*)value; }

            public static readonly IntPtr AbiToProjectionVftablePtr;

            private static Delegate[] DelegateCache = new Delegate[1];

            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    _GetService_0 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new IXamlServiceProvider_Delegates.GetService_0(Do_Abi_GetService_0))
                };
            }

            private static unsafe int Do_Abi_GetService_0(IntPtr thisPtr, global::ABI.System.Type type, IntPtr* result)
            {
                object __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::System.IServiceProvider>(thisPtr).GetService(global::ABI.System.Type.FromAbi(type));
                    *result = MarshalInspectable<object>.FromManaged(__result);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IServiceProvider(IObjectReference obj) => (obj != null) ? new IServiceProvider(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IServiceProvider(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IServiceProvider(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe object GetService(global::System.Type type)
        {
            global::ABI.System.Type.Marshaler __type = default;
            IntPtr __retval = default;
            try
            {
                __type = global::ABI.System.Type.CreateMarshaler(type);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetService_0(ThisPtr, global::ABI.System.Type.GetAbi(__type), &__retval));
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                global::ABI.System.Type.DisposeMarshaler(__type);
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }
    }

    internal static class IXamlServiceProvider_Delegates
    {
        public unsafe delegate int GetService_0(IntPtr thisPtr, global::ABI.System.Type type, IntPtr* result);
    }
}
