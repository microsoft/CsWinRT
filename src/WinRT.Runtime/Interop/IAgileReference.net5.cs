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
                return ComWrappersSupport.GetObjectReferenceForInterface(ptr, riid, false);
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
                return ComWrappersSupport.GetObjectReferenceForInterface<T>(ptr, riid, false);
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
        internal static readonly Guid IID = global::WinRT.Interop.IID.IID_IGlobalInterfaceTable;

        public static ObjectReference<global::WinRT.Interop.IUnknownVftbl> FromAbi(IntPtr thisPtr) => ObjectReference<global::WinRT.Interop.IUnknownVftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_IGlobalInterfaceTable);

        private readonly ObjectReference<global::WinRT.Interop.IUnknownVftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;

        public IGlobalInterfaceTable(IntPtr thisPtr)
        {
            _obj = ObjectReference<global::WinRT.Interop.IUnknownVftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_IGlobalInterfaceTable);
        }

        public IntPtr RegisterInterfaceInGlobal(IntPtr ptr, Guid riid)
        {
            IntPtr thisPtr = ThisPtr;
            IntPtr cookie;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)(*(void***)thisPtr)[3])(thisPtr, ptr, &riid, &cookie));
            return cookie;

        }

        public void RevokeInterfaceFromGlobal(IntPtr cookie)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)(*(void***)thisPtr)[4])(thisPtr, cookie));
        }

        public IObjectReference GetInterfaceFromGlobal(IntPtr cookie, Guid riid)
        {
            IntPtr thisPtr = ThisPtr;
            IntPtr ptr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)(*(void***)thisPtr)[5])(thisPtr, cookie, &riid, &ptr));
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
