﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using global::WinRT;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000038-0000-0000-C000-000000000046")]
#if EMBED
    internal
#else
    public
#endif 
    interface IWeakReferenceSource
    {
        IWeakReference GetWeakReference();
    }

    [WindowsRuntimeType]
    [Guid("00000037-0000-0000-C000-000000000046")]
#if EMBED
    internal
#else
    public
#endif
    interface IWeakReference
    {
        IObjectReference Resolve(Guid riid);
    }

    internal sealed class ManagedWeakReference : IWeakReference
    {
        private readonly WeakReference<object> _ref;
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

            return ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(target, riid);
        }
    }
}


namespace ABI.WinRT.Interop
{
#if EMBED
    internal
#else
    public
#endif
    static class IWeakReferenceSourceMethods
    {
        public static unsafe global::WinRT.Interop.IWeakReference GetWeakReference(IObjectReference _obj)
        {
            var ThisPtr = _obj.ThisPtr;
            IntPtr objRef = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[3](ThisPtr, &objRef));
                return MarshalInterface<global::WinRT.Interop.IWeakReference>.FromAbi(objRef);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(objRef);
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("00000038-0000-0000-C000-000000000046")]
    internal unsafe interface IWeakReferenceSource : global::WinRT.Interop.IWeakReferenceSource
    {
        internal static readonly Guid IID = new(0x00000038, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);

        public static IntPtr AbiToProjectionVftablePtr;
        static unsafe IWeakReferenceSource()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IWeakReferenceSource), sizeof(global::WinRT.Interop.IUnknownVftbl) + sizeof(IntPtr) * 1);
            *(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[3] = &Do_Abi_GetWeakReference;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Do_Abi_GetWeakReference(IntPtr thisPtr, IntPtr* weakReference)
        {
            *weakReference = default;

            try
            {
                using var objRef = ComWrappersSupport.CreateCCWForObject(new global::WinRT.Interop.ManagedWeakReference(ComWrappersSupport.FindObject<object>(thisPtr)));
                ExceptionHelpers.ThrowExceptionForHR(objRef.TryAs(IWeakReference.IID, out var ptr));
                *weakReference = ptr;
            }
            catch (Exception __exception__)
            {
                return __exception__.HResult;
            }
            return 0;
        }

        global::WinRT.Interop.IWeakReference global::WinRT.Interop.IWeakReferenceSource.GetWeakReference()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::WinRT.Interop.IWeakReferenceSource).TypeHandle);
            return IWeakReferenceSourceMethods.GetWeakReference(_obj);
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("00000037-0000-0000-C000-000000000046")]
    internal unsafe interface IWeakReference : global::WinRT.Interop.IWeakReference
    {
        internal static readonly Guid IID = new(0x00000037, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);

        public static IntPtr AbiToProjectionVftablePtr;
        static unsafe IWeakReference()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IWeakReference), sizeof(global::WinRT.Interop.IUnknownVftbl) + sizeof(IntPtr) * 1);
            *(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
            ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>*)AbiToProjectionVftablePtr)[3] = &Do_Abi_Resolve;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        IObjectReference global::WinRT.Interop.IWeakReference.Resolve(Guid riid)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::WinRT.Interop.IWeakReference).TypeHandle);
            var ThisPtr = _obj.ThisPtr;

            IntPtr objRef;
            ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>**)ThisPtr)[3](ThisPtr, &riid, &objRef));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface(objRef);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(objRef);
            }
        }
    }
}
