﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000038-0000-0000-C000-000000000046")]
    internal interface IWeakReferenceSource
    {
        IWeakReference GetWeakReference();
    }

    [WindowsRuntimeType]
    [Guid("00000037-0000-0000-C000-000000000046")]
    internal interface IWeakReference
    {
        IObjectReference Resolve(Guid riid);
    }

    internal class ManagedWeakReference : IWeakReference
    {
        private WeakReference<object> _ref;
        public ManagedWeakReference(object obj)
        {
            _ref = new WeakReference<object>(obj);
        }

        public IObjectReference Resolve(Guid riid)
        {
            if (!_ref.TryGetTarget(out object target))
            {
                return null;
            }

            using (IObjectReference objReference = ComWrappersSupport.CreateCCWForObject(target))
            {
                return objReference.As(riid);
            }
        }
    }
}


namespace ABI.WinRT.Interop
{
    using global::WinRT;
    using WinRT.Interop;

    [Guid("00000038-0000-0000-C000-000000000046")]
    internal unsafe class IWeakReferenceSource : global::WinRT.Interop.IWeakReferenceSource
    {
        [Guid("00000038-0000-0000-C000-000000000046")]
        internal struct Vftbl
        {
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _GetWeakReference;
            public delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int> GetWeakReference { get => (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)_GetWeakReference; set => _GetWeakReference = value; }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if NETSTANDARD2_0
            internal delegate int GetWeakReferenceDelegate(IntPtr thisPtr, IntPtr* weakReference);
            private static readonly Delegate[] DelegateCache = new Delegate[1];
#endif
            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
#if NETSTANDARD2_0
                    _GetWeakReference = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new GetWeakReferenceDelegate(Do_Abi_GetWeakReference)).ToPointer(),
#else
                    _GetWeakReference = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetWeakReference
#endif
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetWeakReference(IntPtr thisPtr, IntPtr* weakReference)
            {
                *weakReference = default;

                try
                {
                    *weakReference = ComWrappersSupport.CreateCCWForObject(new global::WinRT.Interop.ManagedWeakReference(ComWrappersSupport.FindObject<object>(thisPtr))).As<ABI.WinRT.Interop.IWeakReference.Vftbl>().GetRef();
                }
                catch (Exception __exception__)
                {
                    return __exception__.HResult;
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWeakReferenceSource(IObjectReference obj) => (obj != null) ? new IWeakReferenceSource(obj) : null;
        public static implicit operator IWeakReferenceSource(ObjectReference<Vftbl> obj) => (obj != null) ? new IWeakReferenceSource(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IWeakReferenceSource(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IWeakReferenceSource(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public global::WinRT.Interop.IWeakReference GetWeakReference()
        {
            IntPtr objRef = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetWeakReference(ThisPtr, out objRef));
                return MarshalInterface<WinRT.Interop.IWeakReference>.FromAbi(objRef);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(objRef);
            }
        }
    }

    [Guid("00000037-0000-0000-C000-000000000046")]
    internal unsafe class IWeakReference : global::WinRT.Interop.IWeakReference
    {
        [Guid("00000037-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _Resolve;
            public delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int> Resolve { get => (delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int>)_Resolve; set => _Resolve = value; }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if NETSTANDARD2_0
            public delegate int ResolveDelegate(IntPtr thisPtr, Guid* riid, IntPtr* objectReference);
            private static readonly Delegate[] DelegateCache = new Delegate[1];
#endif
            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
#if NETSTANDARD2_0
                    _Resolve = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new ResolveDelegate(Do_Abi_Resolve)).ToPointer(),
#else
                    _Resolve = (delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&Do_Abi_Resolve
#endif
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_Resolve(IntPtr thisPtr, Guid* riid, IntPtr* objectReference)
            {
                IObjectReference _objectReference = default;

                *objectReference = default;

                try
                {
                    _objectReference = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IWeakReference>(thisPtr).Resolve(*riid);
                    *objectReference = _objectReference?.GetRef() ?? IntPtr.Zero;
                }
                catch (Exception __exception__)
                {
                    return __exception__.HResult;
                }
                return 0;
            }
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IWeakReference(IObjectReference obj) => (obj != null) ? new IWeakReference(obj) : null;
        public static implicit operator IWeakReference(ObjectReference<Vftbl> obj) => (obj != null) ? new IWeakReference(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();

        public IWeakReference(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IWeakReference(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IObjectReference Resolve(Guid riid)
        {
            ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Resolve(ThisPtr, ref riid, out IntPtr objRef));
            return ComWrappersSupport.GetObjectReferenceForInterface(objRef);
        }
    }
}
