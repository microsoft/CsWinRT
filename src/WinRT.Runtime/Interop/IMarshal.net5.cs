// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace WinRT.Interop
{
    internal enum MSHCTX : int { Local = 0, NoSharedMem = 1, DifferentMachine = 2, InProc = 3, CrossCtx = 4 }

    internal enum MSHLFLAGS : int { Normal = 0, TableStrong = 1, TableWeak = 2, NoPing = 4 }
}

namespace ABI.WinRT.Interop
{
    [Guid("00000003-0000-0000-c000-000000000046")]
    internal sealed class IMarshal
    {
        private const string NotImplemented_NativeRoutineNotFound = "A native library routine was not found: {0}.";

        private static readonly object _IID_InProcFreeThreadedMarshalerLock = new();
        internal static volatile object _IID_InProcFreeThreadedMarshaler;

        // Simple singleton lazy-initialization scheme (and saving the Lazy<T> size)
        internal static Guid IID_InProcFreeThreadedMarshaler
        {
            get
            {
                object iid = _IID_InProcFreeThreadedMarshaler;

                if (iid is not null)
                {
                    return (Guid)iid;
                }

                return IID_InProcFreeThreadedMarshaler_Slow();

                [MethodImpl(MethodImplOptions.NoInlining)]
                static Guid IID_InProcFreeThreadedMarshaler_Slow()
                {
                    lock (_IID_InProcFreeThreadedMarshalerLock)
                    {
                        return (Guid)(_IID_InProcFreeThreadedMarshaler ??= Vftbl.GetInProcFreeThreadedMarshalerIID());
                    }
                }
            }
        }

        [Guid("00000003-0000-0000-c000-000000000046")]
        public unsafe struct Vftbl
        {
            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::WinRT.Interop.MSHCTX, IntPtr, global::WinRT.Interop.MSHLFLAGS, Guid*, int> GetUnmarshalClass_0;
            public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, global::WinRT.Interop.MSHCTX, IntPtr, global::WinRT.Interop.MSHLFLAGS, uint*, int> GetMarshalSizeMax_1;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr, global::WinRT.Interop.MSHCTX, IntPtr, global::WinRT.Interop.MSHLFLAGS, int> MarshalInterface_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int> UnmarshalInterface_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> ReleaseMarshalData_4;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, int> DisconnectObject_5;

            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.Interop.IUnknownVftbl) + sizeof(IntPtr) * 6);
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
            }

            // This object handles IMarshal calls for us for most scenarios.
            [ThreadStatic]
            private static IMarshal t_freeThreadedMarshaler;

            private static void EnsureHasFreeThreadedMarshaler()
            {
                if (t_freeThreadedMarshaler != null)
                    return;

                try
                {
                    IntPtr proxyPtr;
                    Marshal.ThrowExceptionForHR(Platform.CoCreateFreeThreadedMarshaler(IntPtr.Zero, &proxyPtr));
                    using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref proxyPtr, global::WinRT.Interop.IID.IID_IUnknown);
                    IMarshal proxy = new IMarshal(objRef);
                    t_freeThreadedMarshaler = proxy;
                }
                catch (DllNotFoundException ex)
                {
                    throw new NotImplementedException(string.Format(NotImplemented_NativeRoutineNotFound, "CoCreateFreeThreadedMarshaler"), ex);
                }
            }

            internal static Guid GetInProcFreeThreadedMarshalerIID()
            {
                EnsureHasFreeThreadedMarshaler();

                Guid iid_IUnknown = IID.IID_IUnknown;
                Guid iid_unmarshalClass;
                t_freeThreadedMarshaler.GetUnmarshalClass(&iid_IUnknown, IntPtr.Zero, MSHCTX.InProc, IntPtr.Zero, MSHLFLAGS.Normal, &iid_unmarshalClass);
                return iid_unmarshalClass;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_GetUnmarshalClass_0(IntPtr thisPtr, Guid* riid, IntPtr pv, global::WinRT.Interop.MSHCTX dwDestContext, IntPtr pvDestContext, global::WinRT.Interop.MSHLFLAGS mshlFlags, Guid* pCid)
            {
                *pCid = default;
                try
                {
                    EnsureHasFreeThreadedMarshaler();
                    t_freeThreadedMarshaler.GetUnmarshalClass(riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_GetMarshalSizeMax_1(IntPtr thisPtr, Guid* riid, IntPtr pv, global::WinRT.Interop.MSHCTX dwDestContext, IntPtr pvDestContext, global::WinRT.Interop.MSHLFLAGS mshlflags, uint* pSize)
            {
                *pSize = default;
                try
                {
                    EnsureHasFreeThreadedMarshaler();
                    t_freeThreadedMarshaler.GetMarshalSizeMax(riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_MarshalInterface_2(IntPtr thisPtr, IntPtr pStm, Guid* riid, IntPtr pv, global::WinRT.Interop.MSHCTX dwDestContext, IntPtr pvDestContext, global::WinRT.Interop.MSHLFLAGS mshlflags)
            {
                try
                {
                    EnsureHasFreeThreadedMarshaler();
                    t_freeThreadedMarshaler.MarshalInterface(pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_UnmarshalInterface_3(IntPtr thisPtr, IntPtr pStm, Guid* riid, IntPtr* ppv)
            {
                *ppv = default;
                try
                {
                    EnsureHasFreeThreadedMarshaler();
                    t_freeThreadedMarshaler.UnmarshalInterface(pStm, riid, ppv);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_ReleaseMarshalData_4(IntPtr thisPtr, IntPtr pStm)
            {
                try
                {
                    EnsureHasFreeThreadedMarshaler();
                    t_freeThreadedMarshaler.ReleaseMarshalData(pStm);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static int Do_Abi_DisconnectObject_5(IntPtr thisPtr, uint dwReserved)
            {
                try
                {
                    EnsureHasFreeThreadedMarshaler();
                    t_freeThreadedMarshaler.DisconnectObject(dwReserved);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }
        }

        private readonly ObjectReference<IUnknownVftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public IMarshal(IObjectReference obj)
        {
            _obj = obj.As<IUnknownVftbl>(IID.IID_IMarshal);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void GetUnmarshalClass(Guid* riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlFlags, Guid* pCid)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, MSHCTX, IntPtr, MSHLFLAGS, Guid*, int>)(*(void***)thisPtr)[3])(thisPtr, riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void GetMarshalSizeMax(Guid* riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags, uint* pSize)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, MSHCTX, IntPtr, MSHLFLAGS, uint*, int>)(*(void***)thisPtr)[4])(thisPtr, riid, pv, dwDestContext, pvDestContext, mshlflags, pSize));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void MarshalInterface(IntPtr pStm, Guid* riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr, MSHCTX, IntPtr, MSHLFLAGS, int>)(*(void***)thisPtr)[5])(thisPtr, pStm, riid, pv, dwDestContext, pvDestContext, mshlflags));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void UnmarshalInterface(IntPtr pStm, Guid* riid, IntPtr* ppv)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)(*(void***)thisPtr)[6])(thisPtr, pStm, riid, ppv));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ReleaseMarshalData(IntPtr pStm)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)(*(void***)thisPtr)[7])(thisPtr, pStm));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void DisconnectObject(uint dwReserved)
        {
            IntPtr thisPtr = ThisPtr;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, uint, int>)(*(void***)thisPtr)[8])(thisPtr, dwReserved));
            GC.KeepAlive(_obj);
        }
    }
}