﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
    public enum TrustLevel
    {
        BaseTrust = 0,
        PartialTrust = BaseTrust + 1,
        FullTrust = PartialTrust + 1
    }

    // IInspectable
    [ObjectReferenceWrapper(nameof(_obj))]
    [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
    public partial class IInspectable
    {
        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        public unsafe struct Vftbl
        {
            public IUnknownVftbl IUnknownVftbl;
            private void* _GetIids;
            public delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int> GetIids { get => (delegate* unmanaged[Stdcall]<IntPtr, int*, IntPtr*, int>)_GetIids; set => _GetIids = (void*)value; }
            
            private void* _GetRuntimeClassName;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> GetRuntimeClassName { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_GetRuntimeClassName; set => _GetRuntimeClassName = (void*)value; }
            
            private void* _GetTrustLevel;
            public delegate* unmanaged[Stdcall]<IntPtr, TrustLevel*, int> GetTrustLevel { get => (delegate* unmanaged[Stdcall]<IntPtr, TrustLevel*, int>)_GetTrustLevel; set => _GetTrustLevel = (void*)value; }

            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

#if NETSTANDARD2_0
            private static readonly Delegate[] DelegateCache = new Delegate[3];
            private delegate int _GetIidsDelegate(IntPtr pThis, int* iidCount, IntPtr* iids);
            private delegate int _GetRuntimeClassNameDelegate(IntPtr pThis, IntPtr* className);
            private delegate int _GetTrustLevelDelegate(IntPtr pThis, TrustLevel* trustLevel);
#endif

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = IUnknownVftbl.AbiToProjectionVftbl,
#if NETSTANDARD2_0
                    _GetIids = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new _GetIidsDelegate(Do_Abi_GetIids)),
                    _GetRuntimeClassName = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new _GetRuntimeClassNameDelegate(Do_Abi_GetRuntimeClassName)),
                    _GetTrustLevel = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new _GetTrustLevelDelegate(Do_Abi_GetTrustLevel))
#else
                    _GetIids = (void*)(delegate* unmanaged<IntPtr, int*, IntPtr*, int>)&Do_Abi_GetIids,
                    _GetRuntimeClassName = (void*)(delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetRuntimeClassName,
                    _GetTrustLevel = (void*)(delegate* unmanaged<IntPtr, TrustLevel*, int>)&Do_Abi_GetTrustLevel
#endif
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetIids(IntPtr pThis, int* iidCount, IntPtr* iids)
            {
                *iidCount = 0;
                *iids = IntPtr.Zero;
                try
                {
                    (*iidCount, *iids) = MarshalBlittable<Guid>.FromManagedArray(ComWrappersSupport.GetInspectableInfo(pThis).IIDs);
                }
                catch (Exception ex)
                {
                    return ex.HResult;
                }
                return 0;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private unsafe static int Do_Abi_GetRuntimeClassName(IntPtr pThis, IntPtr* className)
            {
                *className = default;
                try
                {
                    string runtimeClassName = ComWrappersSupport.GetInspectableInfo(pThis).RuntimeClassName;
                    *className = MarshalString.FromManaged(runtimeClassName);
                }
                catch (Exception ex)
                {
                    return ex.HResult;
                }
                return 0;
            }

#if !NETSTANDARD2_0
            [UnmanagedCallersOnly]
#endif
            private static int Do_Abi_GetTrustLevel(IntPtr pThis, TrustLevel* trustLevel)
            {
                *trustLevel = TrustLevel.BaseTrust;
                return 0;
            }
        }

        public static IInspectable FromAbi(IntPtr thisPtr) =>
            new IInspectable(ObjectReference<Vftbl>.FromAbi(thisPtr));

        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public static implicit operator IInspectable(IObjectReference obj) => obj.As<Vftbl>();
        public static implicit operator IInspectable(ObjectReference<Vftbl> obj) => new IInspectable(obj);
        public ObjectReference<I> As<I>() => _obj.As<I>();
        public IObjectReference ObjRef { get => _obj; }
        public IInspectable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IInspectable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe string GetRuntimeClassName(bool noThrow = false)
        {
            IntPtr __retval = default;
            try
            {
                var hr = _obj.Vftbl.GetRuntimeClassName(ThisPtr, &__retval);
                if (hr != 0)
                {
                    if (noThrow)
                        return null;
                    Marshal.ThrowExceptionForHR(hr);
                }
                uint length;
                char* buffer = Platform.WindowsGetStringRawBuffer(__retval, &length);
                return new string(buffer, 0, (int)length);
            }
            finally
            {
                Platform.WindowsDeleteString(__retval);
            }
        }
    }

}
