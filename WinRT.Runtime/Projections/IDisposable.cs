﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.System
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E")]
    public unsafe class IDisposable : global::System.IDisposable
    {
        [Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E")]
        public struct Vftbl
        {
            private delegate int CloseDelegate(IntPtr thisPtr);
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _Close_0;
            public delegate* stdcall<IntPtr, int> Close_0 { get => (delegate* stdcall<IntPtr, int>)_Close_0; set => _Close_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if NETSTANDARD2_0
            private static CloseDelegate closeDelegate;
#endif
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if NETSTANDARD2_0
                    _Close_0 = Marshal.GetFunctionPointerForDelegate(closeDelegate = Do_Abi_Close_0).ToPointer()
#else
                    _Close_0 = (delegate*<IntPtr, int>)&Do_Abi_Close_0
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static unsafe int Do_Abi_Close_0(IntPtr thisPtr)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::System.IDisposable>(thisPtr).Dispose();

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

        public static implicit operator IDisposable(IObjectReference obj) => (obj != null) ? new IDisposable(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IDisposable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IDisposable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe void Dispose()
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Close_0(ThisPtr));
        }
    }
}
