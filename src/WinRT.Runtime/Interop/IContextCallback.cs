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
        public static readonly Guid IID_ICallbackWithNoReentrancyToApplicationSTA = new(0x0A299774, 0x3E4E, 0xFC42, 0x1D, 0x9D, 0x72, 0xCE, 0xE1, 0x05, 0xCA, 0x57);

        private global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, ComCallData*, Guid*, int, IntPtr, int> ContextCallback_4;

        public static void ContextCallback(IntPtr contextCallbackPtr, Action callback, Action onFailCallback)
        {
            ComCallData comCallData;
            comCallData.dwDispid = 0;
            comCallData.dwReserved = 0;
#if NET && CsWinRT_LANG_11_FEATURES

            // We can just store a pointer to the callback to invoke in the context,
            // so we don't need to allocate another closure or anything. The callback
            // will be kept alive automatically, because 'comCallData' is address exposed.
            // We only do this if we can use C# 11, and if we're on modern .NET, to be safe.
            // In the callback below, we can then just retrieve the Action again to invoke it.
            comCallData.pUserDefined = (IntPtr)(void*)&callback;
            
            [UnmanagedCallersOnly]
            static int InvokeCallback(ComCallData* comCallData)
            {
                try
                {
                    // Dereference the pointer to Action and invoke it (see notes above).
                    // Once again, the pointer is not to the Action object, but just to the
                    // local *reference* to the object, which is pinned (as it's a local).
                    // That means that there's no pinning to worry about either.
                    ((Action*)comCallData->pUserDefined)->Invoke();

                    return 0; // S_OK
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }
#else
            comCallData.pUserDefined = IntPtr.Zero;
#endif

            Guid iid = IID_ICallbackWithNoReentrancyToApplicationSTA;
            int hresult;
#if NET && CsWinRT_LANG_11_FEATURES
            hresult = (*(IContextCallbackVftbl**)contextCallbackPtr)->ContextCallback_4(
                contextCallbackPtr,
                (IntPtr)(delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                &comCallData,
                &iid,
                /* iMethod */ 5,
                IntPtr.Zero);
#else
            PFNCONTEXTCALL nativeCallback = _ =>
            {
                callback();
                return 0;
            };

            hresult = (*(IContextCallbackVftbl**)contextCallbackPtr)->ContextCallback_4(
                contextCallbackPtr,
                Marshal.GetFunctionPointerForDelegate(nativeCallback),
                &comCallData,
                &iid,
                /* iMethod */ 5,
                IntPtr.Zero);

            GC.KeepAlive(nativeCallback);
#endif

            if (hresult < 0)
            {
                onFailCallback?.Invoke();
            }
        }
    }
}