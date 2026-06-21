// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

#pragma warning disable CS8500, CS0169

namespace ABI.WinRT.Interop
{
    internal struct ComCallData
    {
        public int dwDispid;
        public int dwReserved;
        public IntPtr pUserDefined;
    }

#if NET && CsWinRT_LANG_11_FEATURES
    internal unsafe struct CallbackData
    {
        public delegate*<object, void> Callback;
        public object State;
    }
#endif

#if NET && CsWinRT_LANG_11_FEATURES
    internal unsafe struct IContextCallbackVftbl
    {
#pragma warning disable CS0649 // Native layout
        private global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, ComCallData*, Guid*, int, IntPtr, int> ContextCallback_4;
#pragma warning restore CS0649

        // Thread-static recursion guard used to validate the concern raised in
        // https://github.com/microsoft/CsWinRT/pull/1865. It tracks the nesting depth of
        // 'ContextCallback' invocations on the current thread, so we can detect whether the
        // dispatch is ever reentered on the same thread (see 'ContextCallback' below).
        [ThreadStatic]
        private static int s_contextCallbackDepth;

        public static void ContextCallback(IntPtr contextCallbackPtr, delegate*<object, void> callback, delegate*<object, void> onFailCallback, object state)
        {
            // Validation guard for https://github.com/microsoft/CsWinRT/pull/1865: detect whether the
            // 'IContextCallback' dispatch can ever be reentered on the same thread (eg. if the native call
            // below pumps messages on an STA thread while waiting for the callback to complete on the target
            // context, and that pump dispatches another call that ends up back here on this same thread). If
            // that ever happens, the thread-static state approach explored in that PR would be unsafe, so we
            // fail fast with a useful message to surface how common this actually is in real world scenarios.
            if (s_contextCallbackDepth > 0)
            {
                Environment.FailFast(
                    "ABI.WinRT.Interop.IContextCallbackVftbl.ContextCallback was invoked recursively on the same thread " +
                    "(managed thread id: " + Environment.CurrentManagedThreadId + ", depth: " + s_contextCallbackDepth + "). The " +
                    "'IContextCallback' dispatch was reentered before a previous call on this thread returned, which would make " +
                    "the thread-static state approach explored in https://github.com/microsoft/CsWinRT/pull/1865 unsafe.");
            }

            ComCallData comCallData;
            comCallData.dwDispid = 0;
            comCallData.dwReserved = 0;

            CallbackData callbackData;
            callbackData.Callback = callback;
            callbackData.State = state;

            // We can just store a pointer to the callback to invoke in the context,
            // so we don't need to allocate another closure or anything. The callback
            // will be kept alive automatically, because 'comCallData' is address exposed.
            // We only do this if we can use C# 11, and if we're on modern .NET, to be safe.
            // In the callback below, we can then just retrieve the Action again to invoke it.
            comCallData.pUserDefined = (IntPtr)(void*)&callbackData;
            
            [UnmanagedCallersOnly]
            static int InvokeCallback(ComCallData* comCallData)
            {
                try
                {
                    CallbackData* callbackData = (CallbackData*)comCallData->pUserDefined;

                    callbackData->Callback(callbackData->State);

                    return 0; // S_OK
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }

            Guid iid = IID.IID_ICallbackWithNoReentrancyToApplicationSTA;

            // Mark that we're now dispatching on this thread. The reentrancy window is the native call
            // below, so we only need to track recursion across it. We always reset the depth afterwards
            // (even if the dispatch throws) so a later, legitimate call on this thread isn't misdetected.
            s_contextCallbackDepth++;

            int hresult;

            try
            {
                hresult = (*(IContextCallbackVftbl**)contextCallbackPtr)->ContextCallback_4(
                    contextCallbackPtr,
                    (IntPtr)(delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                    &comCallData,
                    &iid,
                    /* iMethod */ 5,
                    IntPtr.Zero);
            }
            finally
            {
                s_contextCallbackDepth--;
            }

            if (hresult < 0)
            {
                if (onFailCallback is not null)
                {
                    onFailCallback(state);
                }
            }
        }
    }
#else
    internal unsafe delegate int PFNCONTEXTCALL(ComCallData* data);

    [Guid("000001da-0000-0000-C000-000000000046")]
    internal sealed unsafe class IContextCallback
    {
        internal static readonly Guid IID = global::WinRT.Interop.IID.IID_IContextCallback;

        [Guid("000001da-0000-0000-C000-000000000046")]
        public struct Vftbl
        {
            global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
            private void* _ContextCallback;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, ComCallData*, Guid*, int, IntPtr, int> ContextCallback_4
            {
                get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, ComCallData*, Guid*, int, IntPtr, int>)_ContextCallback;
                set => _ContextCallback = (void*)value;
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IContextCallback(IObjectReference obj) => (obj != null) ? new IContextCallback(obj) : null;
        public static implicit operator IContextCallback(ObjectReference<Vftbl> obj) => (obj != null) ? new IContextCallback(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IContextCallback(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IContextCallback(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe void ContextCallback(PFNCONTEXTCALL pfnCallback, ComCallData* pParam, Guid riid, int iMethod)
        {
            var callback = Marshal.GetFunctionPointerForDelegate(pfnCallback);
            var result = _obj.Vftbl.ContextCallback_4(ThisPtr, callback, pParam, &riid, iMethod, IntPtr.Zero);
            GC.KeepAlive(_obj);
            GC.KeepAlive(pfnCallback);
            Marshal.ThrowExceptionForHR(result);
        }
    }
#endif
}