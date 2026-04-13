// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
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
        [ThreadStatic]
        public static object PerThreadObject;

        public delegate*<object, void> Callback;
        public object* StatePtr;
    }
#endif

#if NET && CsWinRT_LANG_11_FEATURES
    internal unsafe struct IContextCallbackVftbl
    {
#pragma warning disable CS0649 // Native layout
        private global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, ComCallData*, Guid*, int, IntPtr, int> ContextCallback_4;
#pragma warning restore CS0649

        public static void ContextCallback(IntPtr contextCallbackPtr, delegate*<object, void> callback, delegate*<object, void> onFailCallback, object state)
        {
            // Native method that invokes the callback on the target context. The state object
            // is guaranteed to be pinned, so we can access it from a pointer. Note that the
            // object will be stored in a static field, and it will not be on the stack of the
            // original thread, so it's safe with respect to cross-thread access of managed objects.
            // See: https://github.com/dotnet/runtime/blob/main/docs/design/specs/Memory-model.md#cross-thread-access-to-local-variables.
            [UnmanagedCallersOnly]
            static int InvokeCallback(ComCallData* comCallData)
            {
                try
                {
                    CallbackData* callbackData = (CallbackData*)comCallData->pUserDefined;

                    callbackData->Callback(*callbackData->StatePtr);

                    return 0; // S_OK
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }

            // Store the state object in the thread static to pass to the callback.
            // We don't need a volatile write here, we have a memory barrier below.
            CallbackData.PerThreadObject = state;

            int hresult;

            // We use a thread local static field to efficiently store the state that's used by the callback. Note that this
            // is safe with respect to reentrancy, as the target callback will never try to switch back on the original thread.
            // We're only ever switching once on the original context, only to release the object reference that is passed as
            // state. There is no way for that to possibly switch back on the starting thread. As such, using a thread static
            // field to pass the state to the target context (we need to store it somewhere on the managed heap) is fine.
            fixed (object* statePtr = &CallbackData.PerThreadObject)
            {
                CallbackData callbackData;
                callbackData.Callback = callback;
                callbackData.StatePtr = statePtr;

                ComCallData comCallData;
                comCallData.dwDispid = 0;
                comCallData.dwReserved = 0;
                comCallData.pUserDefined = (IntPtr)(void*)&callbackData;

                Guid iid = IID.IID_ICallbackWithNoReentrancyToApplicationSTA;

                // Add a memory barrier to be extra safe that the target thread will be able to see
                // the write we just did on 'PerThreadObject' with the state to pass to the callback.
                Thread.MemoryBarrier();

                hresult = (*(IContextCallbackVftbl**)contextCallbackPtr)->ContextCallback_4(
                    contextCallbackPtr,
                    (IntPtr)(delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                    &comCallData,
                    &iid,
                    /* iMethod */ 5,
                    IntPtr.Zero);
            }

            // Reset the static field to avoid keeping the state alive for longer
            Volatile.Write(ref CallbackData.PerThreadObject, null);

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