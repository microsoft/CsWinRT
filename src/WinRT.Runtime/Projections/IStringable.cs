// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.Windows.Foundation
{
    [Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ManagedIStringableVftbl
    {

        internal IInspectable.Vftbl IInspectableVftbl;
        private void* _ToString_0;
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> ToString_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_ToString_0; set => _ToString_0 = value; }

        private static readonly ManagedIStringableVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
        private unsafe delegate int ToStringDelegate(IntPtr thisPtr, IntPtr* value);
        private static readonly ToStringDelegate delegateCache;
#endif
        static unsafe ManagedIStringableVftbl()
        {
            AbiToProjectionVftable = new ManagedIStringableVftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                _ToString_0 = Marshal.GetFunctionPointerForDelegate(delegateCache = Do_Abi_ToString_0).ToPointer()
#else
                _ToString_0 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_ToString_0
#endif
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(ManagedIStringableVftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(ManagedIStringableVftbl*)nativeVftbl = AbiToProjectionVftable;
            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

#if NET
        [UnmanagedCallersOnly]
#endif
        private static unsafe int Do_Abi_ToString_0(IntPtr thisPtr, IntPtr* value)
        {
            try
            {
                string __value = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).ToString();
                *value = MarshalString.FromManaged(__value);
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