using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

namespace WinRT.Interop
{
    [WindowsRuntimeType]
    [Guid("60D27C8D-5F61-4CCE-B751-690FAE66AA53")]
    public interface IManagedActivationFactory
    {
        void RunClassConstructor();
    }
}

namespace ABI.WinRT.Interop
{
    [DynamicInterfaceCastableImplementation]
    [Guid("60D27C8D-5F61-4CCE-B751-690FAE66AA53")]
    internal unsafe interface IManagedActivationFactory
    {
        public static IntPtr AbiToProjectionVftablePtr;
        static unsafe IManagedActivationFactory()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IManagedActivationFactory), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_RunClassConstructor_0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_RunClassConstructor_0(IntPtr thisPtr)
        {
            try
            {
                global::WinRT.ComWrappersSupport.FindObject<global::WinRT.Interop.IManagedActivationFactory>(thisPtr).RunClassConstructor();

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