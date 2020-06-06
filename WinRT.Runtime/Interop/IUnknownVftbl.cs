using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using QueryInterfaceFtn = delegate* stdcall<IntPtr, ref Guid, out IntPtr, int>;
using AddRefFtn = delegate* stdcall<IntPtr, uint>;
using ReleaseFtn = delegate* stdcall<IntPtr, uint>;

namespace WinRT.Interop
{
    [Guid("00000000-0000-0000-C000-000000000046")]
    public unsafe struct IUnknownVftbl
    {
        private void* _QueryInterface;
        public QueryInterfaceFtn QueryInterface { get => (QueryInterfaceFtn)_QueryInterface; set => _QueryInterface = (void*)value; }
        private void* _AddRef;
        public AddRefFtn AddRef { get => (AddRefFtn)_AddRef; set => _AddRef = (void*)value; }
        private void* _Release;
        public ReleaseFtn Release { get => (ReleaseFtn)_Release; set => _Release = (void*)value; }

        public static readonly IUnknownVftbl AbiToProjectionVftbl;
        public static readonly IntPtr AbiToProjectionVftblPtr;

        static IUnknownVftbl()
        {
            AbiToProjectionVftbl = GetVftbl();
            AbiToProjectionVftblPtr = Marshal.AllocHGlobal(sizeof(IUnknownVftbl));
            Marshal.StructureToPtr(AbiToProjectionVftbl, AbiToProjectionVftblPtr, false);
        }

        private static IUnknownVftbl GetVftbl()
        {
            return ComWrappersSupport.IUnknownVftbl;
        }
    }
}
