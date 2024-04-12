// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("C03F6A43-65A4-9818-987E-E0B810D2A6F2")]
    [WindowsRuntimeHelperType(typeof(global::ABI.WinRT.Interop.IAgileReference))]
    internal interface IAgileReference
    {
        IObjectReference Resolve(Guid riid);
    }

    [WindowsRuntimeType]
    [Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    [WindowsRuntimeHelperType(typeof(global::ABI.WinRT.Interop.IAgileObject))]
#if EMBED
    internal
#else
    public
#endif
    interface IAgileObject
    {
        public static readonly Guid IID = global::WinRT.Interop.IID.IID_IAgileObject;
    }

    [WindowsRuntimeType]
    [Guid("00000146-0000-0000-C000-000000000046")]
    [WindowsRuntimeHelperType(typeof(global::ABI.WinRT.Interop.IGlobalInterfaceTable))]
    internal interface IGlobalInterfaceTable
    {
        IntPtr RegisterInterfaceInGlobal(IntPtr ptr, Guid riid);
        void RevokeInterfaceFromGlobal(IntPtr cookie);
        IObjectReference GetInterfaceFromGlobal(IntPtr cookie, Guid riid);
    }
}

namespace ABI.WinRT.Interop
{
    using global::WinRT;

    internal static class IAgileReferenceMethods
    {
        public static unsafe IObjectReference Resolve(IObjectReference _obj, Guid riid)
        {
            if (_obj == null) return null;

            var ThisPtr = _obj.ThisPtr;
            IntPtr ptr = IntPtr.Zero;
            ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>**)ThisPtr)[3](
                ThisPtr, &riid, &ptr));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface(ptr);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(ptr);
            }
        }

        public static unsafe ObjectReference<T> Resolve<T>(IObjectReference _obj, Guid riid)
        {
            if (_obj == null) return null;

            var ThisPtr = _obj.ThisPtr;
            IntPtr ptr = IntPtr.Zero;
            ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>**)ThisPtr)[3](
                ThisPtr, &riid, &ptr));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface<T>(ptr);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(ptr);
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("C03F6A43-65A4-9818-987E-E0B810D2A6F2")]
    internal unsafe interface IAgileReference : global::WinRT.Interop.IAgileReference
    {
        public static IntPtr AbiToProjectionVftablePtr;
        static unsafe IAgileReference()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IAgileReference), sizeof(global::WinRT.Interop.IUnknownVftbl) + sizeof(IntPtr) * 1);
            *(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
            ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>*)AbiToProjectionVftablePtr)[3] = &Do_Abi_Resolve;
        }

        [UnmanagedCallersOnly]
        private static int Do_Abi_Resolve(IntPtr thisPtr, Guid* riid, IntPtr* objectReference)
        {
            IObjectReference _objectReference = default;

            *objectReference = default;

            try
            {
                _objectReference = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IAgileReference>(thisPtr).Resolve(*riid);
                *objectReference = _objectReference?.GetRef() ?? IntPtr.Zero;
            }
            catch (Exception __exception__)
            {
                return __exception__.HResult;
            }
            return 0;
        }

        IObjectReference global::WinRT.Interop.IAgileReference.Resolve(Guid riid)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::WinRT.Interop.IAgileReference).TypeHandle);
            return IAgileReferenceMethods.Resolve(_obj, riid);
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    interface IAgileObject : global::WinRT.Interop.IAgileObject
    {
        public static IntPtr AbiToProjectionVftablePtr;
        static unsafe IAgileObject()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IAgileObject), sizeof(global::WinRT.Interop.IUnknownVftbl));
            *(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
        }
    }

    [Guid("00000146-0000-0000-C000-000000000046")]
    internal sealed unsafe class IGlobalInterfaceTable : global::WinRT.Interop.IGlobalInterfaceTable
    {
        internal static readonly Guid IID = new(0x00000146, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0x46);

        [Guid("00000146-0000-0000-C000-000000000046")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _RegisterInterfaceInGlobal;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int> RegisterInterfaceInGlobal => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)_RegisterInterfaceInGlobal;
            private void* _RevokeInterfaceFromGlobal;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> RevokeInterfaceFromGlobal => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)_RevokeInterfaceFromGlobal;
            private void* _GetInterfaceFromGlobal;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int> GetInterfaceFromGlobal => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)_GetInterfaceFromGlobal;
        }

        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IGlobalInterfaceTable(IObjectReference obj) => (obj != null) ? new IGlobalInterfaceTable(obj) : null;
        public static implicit operator IGlobalInterfaceTable(ObjectReference<Vftbl> obj) => (obj != null) ? new IGlobalInterfaceTable(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;

        public IGlobalInterfaceTable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IGlobalInterfaceTable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public IntPtr RegisterInterfaceInGlobal(IntPtr ptr, Guid riid)
        {
            IntPtr cookie;
            ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RegisterInterfaceInGlobal(ThisPtr, ptr, &riid, &cookie));
            return cookie;

        }

        public void RevokeInterfaceFromGlobal(IntPtr cookie)
        {
            ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RevokeInterfaceFromGlobal(ThisPtr, cookie));
        }

        public IObjectReference GetInterfaceFromGlobal(IntPtr cookie, Guid riid)
        {
            IntPtr ptr;
            ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetInterfaceFromGlobal(ThisPtr, cookie, &riid, &ptr));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface(ptr);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(ptr);
            }
        }
    }
}
