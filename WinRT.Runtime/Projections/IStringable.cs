using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.Windows.Foundation
{
    [Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ManagedIStringableVftbl
    {
        private unsafe delegate int _ToString_0(IntPtr thisPtr, out IntPtr value);

        internal IInspectable.Vftbl IInspectableVftbl;
        private _ToString_0 ToString_0;

        private static readonly ManagedIStringableVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;
        static unsafe ManagedIStringableVftbl()
        {
            AbiToProjectionVftable = new ManagedIStringableVftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                ToString_0 = Do_Abi_ToString_0
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(ManagedIStringableVftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
            Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        private static unsafe int Do_Abi_ToString_0(IntPtr thisPtr, out IntPtr value)
        {
            value = default;
            try
            {
                string __value = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).ToString();
                value = MarshalString.FromManaged(__value);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }
}
