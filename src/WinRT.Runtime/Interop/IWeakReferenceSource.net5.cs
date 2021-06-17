using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000038-0000-0000-C000-000000000046")]
    public interface IWeakReferenceSource
    {
        IWeakReference GetWeakReference();
    }

    [WindowsRuntimeType]
    [Guid("00000037-0000-0000-C000-000000000046")]
    public interface IWeakReference
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

    [DynamicInterfaceCastableImplementation]
    [Guid("00000038-0000-0000-C000-000000000046")]
    internal unsafe interface IWeakReferenceSource : global::WinRT.Interop.IWeakReferenceSource
    {
        [Guid("00000038-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _GetWeakReference;
            public delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int> GetWeakReference { get => (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)_GetWeakReference; set => _GetWeakReference = value; }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,

                    _GetWeakReference = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetWeakReference

                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }


            [UnmanagedCallersOnly]

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

        global::WinRT.Interop.IWeakReference global::WinRT.Interop.IWeakReferenceSource.GetWeakReference()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::WinRT.Interop.IWeakReferenceSource).TypeHandle));
            var ThisPtr = _obj.ThisPtr;

            IntPtr objRef = IntPtr.Zero;
            try
            {
                ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetWeakReference(ThisPtr, out objRef));
                return MarshalInterface<global::WinRT.Interop.IWeakReference>.FromAbi(objRef);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(objRef);
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("00000037-0000-0000-C000-000000000046")]
    internal unsafe interface IWeakReference : global::WinRT.Interop.IWeakReference
    {
        [Guid("00000037-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _Resolve;
            public delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int> Resolve { get => (delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int>)_Resolve; set => _Resolve = value; }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;


            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,

                    _Resolve = (delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&Do_Abi_Resolve

                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }


            [UnmanagedCallersOnly]

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

        IObjectReference global::WinRT.Interop.IWeakReference.Resolve(Guid riid)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::WinRT.Interop.IWeakReference).TypeHandle));
            var ThisPtr = _obj.ThisPtr;

            ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Resolve(ThisPtr, ref riid, out IntPtr objRef));
            return ComWrappersSupport.GetObjectReferenceForInterface(objRef);
        }
    }
}
