// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using global::WinRT;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000038-0000-0000-C000-000000000046")]
    [WindowsRuntimeHelperType(typeof(global::ABI.WinRT.Interop.IWeakReferenceSource))]
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
    [WindowsRuntimeHelperType(typeof(global::ABI.WinRT.Interop.IWeakReference))]
#if EMBED
    internal
#else
    public
#endif
    interface IWeakReference
    {
        IObjectReference Resolve(Guid riid);
    }

    [WinRTExposedType(typeof(ManagedWeakReferenceTypeDetails))]
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

        public IntPtr ResolveForABI(Guid riid)
        {
            if (!_ref.TryGetTarget(out object target))
            {
                return IntPtr.Zero;
            }

            return ComWrappersSupport.CreateCCWForObjectForABI(target, riid);
        }
    }

    internal sealed class ManagedWeakReferenceTypeDetails : IWinRTExposedTypeDetails
    {
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            return new ComWrappers.ComInterfaceEntry[]
            {
                new ComWrappers.ComInterfaceEntry
                {
                    IID = IID.IID_IWeakReference,
                    Vtable = ABI.WinRT.Interop.IWeakReference.AbiToProjectionVftablePtr
                }
            };
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe global::WinRT.Interop.IWeakReference GetWeakReference(IObjectReference _obj)
        {
            var ThisPtr = _obj.ThisPtr;
            IntPtr objRef = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[3](ThisPtr, &objRef));
                GC.KeepAlive(_obj);
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
                *weakReference = ComWrappersSupport.CreateCCWForObjectForABI(new global::WinRT.Interop.ManagedWeakReference(ComWrappersSupport.FindObject<object>(thisPtr)), global::WinRT.Interop.IID.IID_IWeakReference);
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
            *objectReference = default;

            try
            {
                var weakReference = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IWeakReference>(thisPtr);
                if (weakReference is global::WinRT.Interop.ManagedWeakReference managedWeakReference)
                {
                    *objectReference = managedWeakReference.ResolveForABI(*riid);
                }
                else
                {
                    using var _objectReference = weakReference.Resolve(*riid);
                    *objectReference = _objectReference?.GetRef() ?? IntPtr.Zero;
                }
            }
            catch (Exception __exception__)
            {
                return __exception__.HResult;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        IObjectReference global::WinRT.Interop.IWeakReference.Resolve(Guid riid)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::WinRT.Interop.IWeakReference).TypeHandle);
            var ThisPtr = _obj.ThisPtr;

            IntPtr objRef;
            ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>**)ThisPtr)[3](ThisPtr, &riid, &objRef));
            GC.KeepAlive(_obj);
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface(objRef, riid, requireQI: false);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(objRef);
            }
        }
    }
}
