// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.System
{
#if EMBED
    internal
#else
    public
#endif
    static class IServiceProviderMethods
    {
        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xDF, 0xA2, 0xB3, 0x68, 0x73, 0x81, 0x9F, 0x53, 0xB5, 0x24, 0xC8, 0xA2, 0x34, 0x8F, 0x5A, 0xFB }));

        public static IntPtr AbiToProjectionVftablePtr => IServiceProvider.AbiToProjectionVftablePtr;

        public static unsafe object GetService(IObjectReference obj, global::System.Type type)
        {
            global::ABI.System.Type.Marshaler __type = default;
            IntPtr __retval = default;
            try
            {
                var ThisPtr = obj.ThisPtr;
                __type = global::ABI.System.Type.CreateMarshaler(type);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, global::ABI.System.Type, IntPtr*, int>**)ThisPtr)[6](
                    ThisPtr,
                    global::ABI.System.Type.GetAbi(__type),
                    &__retval));
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                global::ABI.System.Type.DisposeMarshaler(__type);
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }
    }

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IServiceProvider : global::System.IServiceProvider
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe IServiceProvider()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IServiceProvider), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, global::ABI.System.Type, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_GetService_0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        unsafe object global::System.IServiceProvider.GetService(global::System.Type type)
        {
            var obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.IServiceProvider).TypeHandle);
            return IServiceProviderMethods.GetService(obj, type);
        }
    }

    internal static class IXamlServiceProvider_Delegates
    {
        public unsafe delegate int GetService_0(IntPtr thisPtr, global::ABI.System.Type type, IntPtr* result);
    }
}
