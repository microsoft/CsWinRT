// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
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
    static class IDisposableMethods
    {
        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x29, 0xA8, 0xD5, 0x30, 0xA4, 0x7F, 0x26, 0x40, 0x83, 0xBB, 0xD7, 0x5B, 0xAE, 0x4E, 0xA9, 0x9E }));

        public static IntPtr AbiToProjectionVftablePtr => IDisposable.AbiToProjectionVftablePtr;

        public static unsafe void Dispose(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[6](ThisPtr));
        }
    }

    [DynamicInterfaceCastableImplementation]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E")]
    internal unsafe interface IDisposable : global::System.IDisposable
    {
        public readonly static IntPtr AbiToProjectionVftablePtr;

        static unsafe IDisposable()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IDisposable), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_Close_0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_Close_0(IntPtr thisPtr)
        {
            try
            {
                global::WinRT.ComWrappersSupport.FindObject<global::System.IDisposable>(thisPtr).Dispose();

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        unsafe void global::System.IDisposable.Dispose()
        {
            var obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.IDisposable).TypeHandle);
            IDisposableMethods.Dispose(obj);
        }
    }
}
