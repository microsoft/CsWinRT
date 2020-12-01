using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using WinRT;
using WinRT.Interop;

namespace ABI.System
{
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IServiceProvider : global::System.IServiceProvider
    {
        [Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public delegate* unmanaged<IntPtr, global::ABI.System.Type, IntPtr*, int> GetService_0;

            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetService_0 = (delegate* unmanaged<IntPtr, global::ABI.System.Type, IntPtr*, int>)&Do_Abi_GetService_0
                };
            }

            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_GetService_0(IntPtr thisPtr, global::ABI.System.Type type, IntPtr* result)
            {
                object __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::System.IServiceProvider>(thisPtr).GetService(global::ABI.System.Type.FromAbi(type));
                    *result = MarshalInspectable<object>.FromManaged(__result);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        unsafe object global::System.IServiceProvider.GetService(global::System.Type type)
        {
            global::ABI.System.Type.Marshaler __type = default;
            IntPtr __retval = default;
            try
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.IServiceProvider).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                __type = global::ABI.System.Type.CreateMarshaler(type);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetService_0(ThisPtr, global::ABI.System.Type.GetAbi(__type), &__retval));
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                global::ABI.System.Type.DisposeMarshaler(__type);
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }
    }

    internal static class IXamlServiceProvider_Delegates
    {
        public unsafe delegate int GetService_0(IntPtr thisPtr, global::ABI.System.Type type, IntPtr* result);
    }
}
