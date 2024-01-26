// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000035-0000-0000-C000-000000000046")]
    [WindowsRuntimeHelperType(typeof(global::ABI.WinRT.Interop.IActivationFactory))]
#if EMBED
    internal
#else
    public
#endif
    interface IActivationFactory
    {
        IntPtr ActivateInstance();
    }
}

namespace ABI.WinRT.Interop
{
#if EMBED 
    internal
#else
    public
#endif
    static class IActivationFactoryMethods
    {
#if NET
        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x35, 0, 0, 0, 0, 0, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46 }));
#else
        public static global::System.Guid IID { get; } = new(0x00000035, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
#endif

        public static IntPtr AbiToProjectionVftablePtr => IActivationFactory.Vftbl.AbiToProjectionVftablePtr;
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("00000035-0000-0000-C000-000000000046")]
#if EMBED
    internal
#else
    public 
#endif
    unsafe class IActivationFactory : global::WinRT.Interop.IActivationFactory
    {
        [Guid("00000035-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
#if !NET
            private delegate int ActivateInstance_Delegate(IntPtr thisPtr, IntPtr* pobj);
            private void* _ActivateInstance_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> ActivateInstance_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_ActivateInstance_0; set => _ActivateInstance_0 = value; }
#else
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> ActivateInstance_0;
#endif
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private static readonly Delegate[] DelegateCache = new Delegate[1];
#endif
            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _ActivateInstance_0 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new ActivateInstance_Delegate(Do_Abi_ActivateInstance_0))
#else
                    ActivateInstance_0 = &Do_Abi_ActivateInstance_0
#endif
                };
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new [] { typeof(CallConvStdcall) })]
#endif
            private static unsafe int Do_Abi_ActivateInstance_0(IntPtr thisPtr, IntPtr* result)
            {
                IntPtr __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IActivationFactory>(thisPtr).ActivateInstance();
                    *result = __result;
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IActivationFactory(IObjectReference obj) => (obj != null) ? new IActivationFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IActivationFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IActivationFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IntPtr ActivateInstance()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.ActivateInstance_0(ThisPtr, &__retval));
                return __retval;
            }
            finally
            {
            }
        }
    }
}
