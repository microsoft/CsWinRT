using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000035-0000-0000-C000-000000000046")]
    public interface IActivationFactory
    {
        IntPtr ActivateInstance();
    }
}

namespace ABI.WinRT.Interop
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("00000035-0000-0000-C000-000000000046")]
    public unsafe class IActivationFactory : global::WinRT.Interop.IActivationFactory
    {
        [Guid("00000035-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private delegate*<IntPtr, IntPtr*, int> _ActivateInstance_0;
            public delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int> ActivateInstance_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)_ActivateInstance_0; set => _ActivateInstance_0 = (delegate*<IntPtr, IntPtr*, int>)value; }

            public static readonly IntPtr AbiToProjectionVftablePtr;

#if NETSTANDARD2_0
            private static readonly Delegate[] DelegateCache = new Delegate[1];
#endif
            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    _ActivateInstance_0 = &Do_Abi_ActivateInstance_0
                };
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
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
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.ActivateInstance_0(ThisPtr, out __retval));
                return __retval;
            }
            finally
            {
            }
        }
    }
}
