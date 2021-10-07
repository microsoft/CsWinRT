using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [Guid("00000000-0000-0000-C000-000000000046")]
    public unsafe struct IUnknownVftbl
    {
        private void* _QueryInterface;
        public delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int> QueryInterface { get => (delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int>)_QueryInterface; set => _QueryInterface = (void*)value; }
        private void* _AddRef;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> AddRef { get => (delegate* unmanaged[Stdcall]<IntPtr, uint>)_AddRef; set => _AddRef = (void*)value; }
        private void* _Release;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> Release { get => (delegate* unmanaged[Stdcall]<IntPtr, uint>)_Release; set => _Release = (void*)value; }

        public static IUnknownVftbl AbiToProjectionVftbl => ComWrappersSupport.IUnknownVftbl;
        public static IntPtr AbiToProjectionVftblPtr => ComWrappersSupport.IUnknownVftblPtr;

        internal static readonly Guid IID = Guid.Parse("00000000-0000-0000-C000-000000000046");
    }
}
