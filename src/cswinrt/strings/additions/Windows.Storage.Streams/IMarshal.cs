namespace Com
{
    using global::System;

    internal enum MSHCTX : int { Local = 0, NoSharedMem = 1, DifferentMachine = 2, InProc = 3, CrossCtx = 4 }
    internal enum MSHLFLAGS : int { Normal = 0, TableStrong = 1, TableWeak = 2, NoPing = 4 }

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
    [Guid("00000003-0000-0000-c000-000000000046")]
    internal interface IMarshal
    {
        void GetUnmarshalClass(ref Guid riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlFlags, out Guid pCid);

        void GetMarshalSizeMax(ref Guid riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags, out uint pSize);

        void MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, MSHCTX dwDestContext, IntPtr pvDestContext, MSHLFLAGS mshlflags);

        void UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv);

        void ReleaseMarshalData(IntPtr pStm);

        void DisconnectObject(uint dwReserved);
    }
}

namespace ABI.Com
{
    using global::System;
    using global::System.Runtime.InteropServices;

    [Guid("00000003-0000-0000-c000-000000000046")]
    internal class IMarshal : global::Com.IMarshal
    {
        [Guid("00000003-0000-0000-c000-000000000046")]
        public struct Vftbl
        {
            public delegate int _GetUnmarshalClass_0(IntPtr thisPtr, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlFlags, out Guid pCid);
            public delegate int _GetMarshalSizeMax_1(IntPtr thisPtr, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags, out uint pSize);
            public delegate int _MarshalInterface_2(IntPtr thisPtr, IntPtr pStm, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags);
            public delegate int _UnmarshalInterface_3(IntPtr thisPtr, IntPtr pStm, ref Guid riid, out IntPtr ppv);
            public delegate int _ReleaseMarshalData_4(IntPtr thisPtr, IntPtr pStm);
            public delegate int _DisconnectObject_5(IntPtr thisPtr, uint dwReserved);

            internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            public _GetUnmarshalClass_0 GetUnmarshalClass_0;
            public _GetMarshalSizeMax_1 GetMarshalSizeMax_1;
            public _MarshalInterface_2 MarshalInterface_2;
            public _UnmarshalInterface_3 UnmarshalInterface_3;
            public _ReleaseMarshalData_4 ReleaseMarshalData_4;
            public _DisconnectObject_5 DisconnectObject_5;


            public static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                    GetUnmarshalClass_0 = Do_Abi_GetUnmarshalClass_0,
                    GetMarshalSizeMax_1 = Do_Abi_GetMarshalSizeMax_1,
                    MarshalInterface_2 = Do_Abi_MarshalInterface_2,
                    UnmarshalInterface_3 = Do_Abi_UnmarshalInterface_3,
                    ReleaseMarshalData_4 = Do_Abi_ReleaseMarshalData_4,
                    DisconnectObject_5 = Do_Abi_DisconnectObject_5
                };
                AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
            }

            public Vftbl(IntPtr ptr)
            {
                this = Marshal.PtrToStructure<Vftbl>(ptr);
            }

            private static int Do_Abi_GetUnmarshalClass_0(IntPtr thisPtr, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlFlags, out Guid pCid)
            {
                pCid = default;
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).GetUnmarshalClass(ref riid, pv, dwDestContext, pvDestContext, mshlFlags, out pCid);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_GetMarshalSizeMax_1(IntPtr thisPtr, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags, out uint pSize)
            {
                pSize = default;
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).GetMarshalSizeMax(ref riid, pv, dwDestContext, pvDestContext, mshlflags, out pSize);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_MarshalInterface_2(IntPtr thisPtr, IntPtr pStm, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags)
            {
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).MarshalInterface(pStm, ref riid, pv, dwDestContext, pvDestContext, mshlflags);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

            private static int Do_Abi_UnmarshalInterface_3(IntPtr thisPtr, IntPtr pStm, ref Guid riid, out IntPtr ppv)
            {
                ppv = default;
                try
                {
                    ComWrappersSupport.FindObject<global::Com.IMarshal>(thisPtr).UnmarshalInterface(pStm, ref riid, out ppv);
                }
                catch (Exception ex)
                {
                    return Marshal.GetHRForException(ex);
                }
                return 0;
            }

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
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IMarshal(IObjectReference obj) => (obj != null) ? new IMarshal(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IMarshal(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IMarshal(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public void GetUnmarshalClass(ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlFlags, out Guid pCid)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetUnmarshalClass_0(ThisPtr, ref riid, pv, dwDestContext, pvDestContext, mshlFlags, out pCid));
        }

        public void GetMarshalSizeMax(ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags, out uint pSize)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.GetMarshalSizeMax_1(ThisPtr, ref riid, pv, dwDestContext, pvDestContext, mshlflags, out pSize));
        }

        public void MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, global::Com.MSHCTX dwDestContext, IntPtr pvDestContext, global::Com.MSHLFLAGS mshlflags)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.MarshalInterface_2(ThisPtr, pStm, ref riid, pv, dwDestContext, pvDestContext, mshlflags));
        }

        public void UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.UnmarshalInterface_3(ThisPtr, pStm, ref riid, out ppv));
        }

        public void ReleaseMarshalData(IntPtr pStm)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.ReleaseMarshalData_4(ThisPtr, pStm));
        }

        public void DisconnectObject(uint dwReserved)
        {
            Marshal.ThrowExceptionForHR(_obj.Vftbl.DisconnectObject_5(ThisPtr, dwReserved));
        }
    }
}