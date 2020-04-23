using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [Guid("00000038-0000-0000-C000-000000000046")]
    internal struct IWeakReferenceSourceVftbl
    {
        public delegate int _GetWeakReference(IntPtr thisPtr, out IntPtr weakReference);

        public IUnknownVftbl IUnknownVftbl;
        public _GetWeakReference GetWeakReference;

        public static readonly IWeakReferenceSourceVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static IWeakReferenceSourceVftbl()
        {
            AbiToProjectionVftable = new IWeakReferenceSourceVftbl
            {
                IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
                GetWeakReference = Do_Abi_GetWeakReference
            };
            AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<IWeakReferenceSourceVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
        }

        private static int Do_Abi_GetWeakReference(IntPtr thisPtr, out IntPtr weakReference)
        {
            weakReference = default;

            try
            {
                weakReference = ComWrappersSupport.CreateCCWForObject(new ManagedWeakReference(ComWrappersSupport.FindObject<object>(thisPtr))).As<ABI.WinRT.Interop.IWeakReference.Vftbl>().GetRef();
            }
            catch (Exception __exception__)
            {
                return __exception__.HResult;
            }
            return 0;
        }
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

    [Guid("00000037-0000-0000-C000-000000000046")]
    internal class IWeakReference
    {
        [Guid("00000037-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            public delegate int _Resolve(IntPtr thisPtr, ref Guid riid, out IntPtr objectReference);

            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _Resolve Resolve;

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    Resolve = Do_Abi_Resolve
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            private static int Do_Abi_Resolve(IntPtr thisPtr, ref Guid riid, out IntPtr objectReference)
            {
                IObjectReference _objectReference = default;

                objectReference = default;

                try
                {
                    _objectReference = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IWeakReference>(thisPtr).Resolve(riid);
                    objectReference = _objectReference?.GetRef() ?? IntPtr.Zero;
                }
                catch (Exception __exception__)
                {
                    return __exception__.HResult;
                }
                return 0;
            }
        }
    }
}
