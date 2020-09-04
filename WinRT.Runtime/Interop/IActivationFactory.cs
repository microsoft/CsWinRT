using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("00000035-0000-0000-C000-000000000046")]
    public interface IActivationFactory
    {
        IntPtr ActivateInstance();
    }
}

namespace ABI.WinRT.Interop
{ 
    [Guid("00000035-0000-0000-C000-000000000046")]
    public class IActivationFactory
    {
        [Guid("00000035-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            public unsafe delegate int _ActivateInstance(IntPtr pThis, out IntPtr instance);

            internal IInspectable.Vftbl IInspectableVftbl;
            public _ActivateInstance ActivateInstance;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    ActivateInstance = Do_Abi_ActivateInstance
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_ActivateInstance(IntPtr thisPtr, out IntPtr result)
            {
                IntPtr __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IActivationFactory>(thisPtr).ActivateInstance();
                    result = __result;

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
}
