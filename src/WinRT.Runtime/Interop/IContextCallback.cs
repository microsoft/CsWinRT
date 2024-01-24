// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

#pragma warning disable CS8500

namespace ABI.WinRT.Interop
{
    internal struct ComCallData
    {
        public int dwDispid;
        public int dwReserved;
        public IntPtr pUserDefined;
    }

#if !(NET && CsWinRT_LANG_11_FEATURES)
    internal unsafe delegate int PFNCONTEXTCALL(ComCallData* data);
#endif

    internal unsafe struct IContextCallbackVftbl
    {
        internal static readonly Guid IID = InterfaceIIDs.IContextCallback_IID;
        internal static readonly Guid IID_ICallbackWithNoReentrancyToApplicationSTA = new(0x0A299774, 0x3E4E, 0xFC42, 0x1D, 0x9D, 0x72, 0xCE, 0xE1, 0x05, 0xCA, 0x57);

        private global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, ComCallData*, Guid*, int, IntPtr, int> ContextCallback_4;

        public void ContextCallback(IntPtr contextCallbackPtr, Action callback, Action onFailCallback)
        {
            // We can just store a pointer to the callback to invoke in the context,
            // so we don't need to allocate another closure or anything. The callback
            // will be kept alive automatically, because 'comCallData' is address exposed.
            // We only do this if we can use C# 11, and if we're on modern .NET, to be safe.
            ComCallData comCallData;
            comCallData.dwDispid = 0;
            comCallData.dwReserved = 0;
#if NET && CsWinRT_LANG_11_FEATURES
            comCallData.pUserDefined = (IntPtr)(void*)&callback;
#else
            comCallData.pUserDefined = IntPtr.Zero;
#endif

            int hresult;
#if NET && CsWinRT_LANG_11_FEATURES
            hresult = ContextCallback_4(
                contextCallbackPtr,
                (IntPtr)(delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                &comCallData,
                (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_ICallbackWithNoReentrancyToApplicationSTA)),
                /* iMethod */ 5,
                IntPtr.Zero);
#else
            PFNCONTEXTCALL nativeCallback = _ =>
            {
                callback();
                return 0;
            };

            hresult = ContextCallback_4(
                contextCallbackPtr,
                Marshal.GetFunctionPointerForDelegate(nativeCallback),
                &comCallData,
                (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID_ICallbackWithNoReentrancyToApplicationSTA)),
                /* iMethod */ 5,
                IntPtr.Zero);

            GC.KeepAlive(nativeCallback);
#endif

            if (hresult < 0)
            {
                onFailCallback?.Invoke();
            }
        }

#if NET && CsWinRT_LANG_11_FEATURES
        [UnmanagedCallersOnly]
        private static int InvokeCallback(ComCallData* comCallData)
        {
            try
            {
                ((Action*)comCallData->pUserDefined)->Invoke();

                return 0; // S_OK
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }
#endif
    }
}