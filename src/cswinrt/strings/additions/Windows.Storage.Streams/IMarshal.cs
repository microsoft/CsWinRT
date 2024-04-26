namespace Com
{
    using global::System;

    internal enum MSHCTX : int { Local = 0, NoSharedMem = 1, DifferentMachine = 2, InProc = 3, CrossCtx = 4 }
    internal enum MSHLFLAGS : int { Normal = 0, TableStrong = 1, TableWeak = 2, NoPing = 4 }

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
    [Guid("00000003-0000-0000-c000-000000000046")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Com.IMarshal))]
    internal interface IMarshal
    {
        unsafe void GetUnmarshalClass(Guid* riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlFlags, Guid* pCid);

        unsafe void GetMarshalSizeMax(Guid* riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags, uint* pSize);

        unsafe void MarshalInterface(IntPtr pStm, Guid* riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags);

        unsafe void UnmarshalInterface(IntPtr pStm, Guid* riid, IntPtr* ppv);

        void ReleaseMarshalData(IntPtr pStm);

        void DisconnectObject(uint dwReserved);
    }
}

namespace ABI.Com
{
    using global::System;
    using global::System.ComponentModel;
    using global::System.Diagnostics.CodeAnalysis;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;

    [Guid("00000003-0000-0000-c000-000000000046")]
    internal sealed class IMarshal : global::Com.IMarshal
    {
        [Guid("00000003-0000-0000-c000-000000000046")]
        public unsafe struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;

#if !NET
            private void* _GetUnmarshalClass_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, Guid*, int> GetUnmarshalClass_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, Guid*, int>)_GetUnmarshalClass_0; set => _GetUnmarshalClass_0 = value; }
            private void* _GetMarshalSizeMax_1;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, uint*, int> GetMarshalSizeMax_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, uint*, int>)_GetMarshalSizeMax_1; set => _GetMarshalSizeMax_1 = value; }
            private void* _MarshalInterface_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, int> MarshalInterface_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, int>)_MarshalInterface_2; set => _MarshalInterface_2 = value; }
            private void* _UnmarshalInterface_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int> UnmarshalInterface_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)_UnmarshalInterface_3; set => _UnmarshalInterface_3 = value; }
            private void* _ReleaseMarshalData_4;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> ReleaseMarshalData_4 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)_ReleaseMarshalData_4; set => _ReleaseMarshalData_4 = value; }
            private void* _DisconnectObject_5;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, int> DisconnectObject_5 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, int>)_DisconnectObject_5; set => _DisconnectObject_5 = value; }

            private static readonly Delegate[] DelegateCache = new Delegate[6];
            public static readonly Vftbl AbiToProjectionVftable;
#else
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, Guid*, int> GetUnmarshalClass_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, uint*, int> GetMarshalSizeMax_1;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr, global::Com.MSHCTX, IntPtr, global::Com.MSHLFLAGS, int> MarshalInterface_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int> UnmarshalInterface_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> ReleaseMarshalData_4;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, int> DisconnectObject_5;
#endif

            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
#if !NET
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    _GetUnmarshalClass_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new IMarshal_Delegates.GetUnmarshalClass_0(Do_Abi_GetUnmarshalClass_0)).ToPointer(),
                    _GetMarshalSizeMax_1 = Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IMarshal_Delegates.GetMarshalSizeMax_1(Do_Abi_GetMarshalSizeMax_1)).ToPointer(),
                    _MarshalInterface_2 = Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new IMarshal_Delegates.MarshalInterface_2(Do_Abi_MarshalInterface_2)).ToPointer(),
                    _UnmarshalInterface_3 = Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new IMarshal_Delegates.UnmarshalInterface_3(Do_Abi_UnmarshalInterface_3)).ToPointer(),
                    _ReleaseMarshalData_4 = Marshal.GetFunctionPointerForDelegate(DelegateCache[4] = new IMarshal_Delegates.ReleaseMarshalData_4(Do_Abi_ReleaseMarshalData_4)).ToPointer(),
                    _DisconnectObject_5 = Marshal.GetFunctionPointerForDelegate(DelegateCache[5] = new IMarshal_Delegates.DisconnectObject_5(Do_Abi_DisconnectObject_5)).ToPointer(),
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
#else
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.Interop.IUnknownVftbl>() + sizeof(IntPtr) * 6);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    GetUnmarshalClass_0 = &Do_Abi_GetUnmarshalClass_0,
                    GetMarshalSizeMax_1 = &Do_Abi_GetMarshalSizeMax_1,
                    MarshalInterface_2 = &Do_Abi_MarshalInterface_2,
                    UnmarshalInterface_3 = &Do_Abi_UnmarshalInterface_3,
                    ReleaseMarshalData_4 = &Do_Abi_ReleaseMarshalData_4,
                    DisconnectObject_5 = &Do_Abi_DisconnectObject_5
                };
#endif
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
            private static int Do_Abi_GetUnmarshalClass_0(IntPtr thisPtr, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlFlags, Guid* pCid)
            {
                *pCid = default;
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).GetUnmarshalClass(riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
            private static int Do_Abi_GetMarshalSizeMax_1(IntPtr thisPtr, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags, uint* pSize)
            {
                *pSize = default;
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).GetMarshalSizeMax(riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
            private static int Do_Abi_MarshalInterface_2(IntPtr thisPtr, IntPtr pStm, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags)
            {
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).MarshalInterface(pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
            private static int Do_Abi_UnmarshalInterface_3(IntPtr thisPtr, IntPtr pStm, Guid* riid, IntPtr* ppv)
            {
                *ppv = default;
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).UnmarshalInterface(pStm, riid, ppv);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
            private static int Do_Abi_ReleaseMarshalData_4(IntPtr thisPtr, IntPtr pStm)
            {
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).ReleaseMarshalData(pStm);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

#if NET
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
            private static int Do_Abi_DisconnectObject_5(IntPtr thisPtr, uint dwReserved)
            {
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).DisconnectObject(dwReserved);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_IMarshal);

        public static implicit operator IMarshal(IObjectReference obj) => (obj != null) ? new IMarshal(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;

#if NET
        [Obsolete("This method is deprecated and will be removed in a future release.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "This method is not trim-safe, and is only supported for use when not using trimming (or AOT).")]
#endif
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IMarshal(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IMarshal(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe void GetUnmarshalClass(Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlFlags, Guid* pCid)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetUnmarshalClass_0(ThisPtr, riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid));
        }

        public unsafe void GetMarshalSizeMax(Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags, uint* pSize)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetMarshalSizeMax_1(ThisPtr, riid, pv, dwDestContext, pvDestContext, mshlflags, pSize));
        }

        public unsafe void MarshalInterface(IntPtr pStm, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.MarshalInterface_2(ThisPtr, pStm, riid, pv, dwDestContext, pvDestContext, mshlflags));
        }

        public unsafe void UnmarshalInterface(IntPtr pStm, Guid* riid, IntPtr* ppv)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.UnmarshalInterface_3(ThisPtr, pStm, riid, ppv));
        }

        public unsafe void ReleaseMarshalData(IntPtr pStm)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.ReleaseMarshalData_4(ThisPtr, pStm));
        }

        public unsafe void DisconnectObject(uint dwReserved)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.DisconnectObject_5(ThisPtr, dwReserved));
        }
    }

    internal static unsafe class IMarshal_Delegates
    {
        public delegate int GetUnmarshalClass_0(IntPtr thisPtr, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlFlags, Guid* pCid);
        public delegate int GetMarshalSizeMax_1(IntPtr thisPtr, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags, uint* pSize);
        public delegate int MarshalInterface_2(IntPtr thisPtr, IntPtr pStm, Guid* riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags);
        public delegate int UnmarshalInterface_3(IntPtr thisPtr, IntPtr pStm, Guid* riid, IntPtr* ppv);
        public delegate int ReleaseMarshalData_4(IntPtr thisPtr, IntPtr pStm);
        public delegate int DisconnectObject_5(IntPtr thisPtr, uint dwReserved);
    }
}