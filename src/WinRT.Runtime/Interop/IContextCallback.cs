// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
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
    internal sealed unsafe class CallbackData
    {
        [ThreadStatic]
        private static CallbackData TlsInstance;

        public delegate*<object, void> Callback;
        public object State;
        public GCHandle Handle;

        private CallbackData()
        {
            // Create a handle to access the object from a native callback invoked on another thread.
            // The handle is weak to ensure that the object does not leak (or it would keep itself
            // alive). The target is guaranteed to be alive because callers will use 'GC.KeepAlive'.
            Handle = GCHandle.Alloc(this, GCHandleType.Weak);
        }

        ~CallbackData()
        {
            Handle.Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallbackData GetOrCreate()
        {
            return TlsInstance ??= new CallbackData();
        }
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
            ComCallData comCallData;
            comCallData.dwDispid = 0;
            comCallData.dwReserved = 0;

            CallbackData callbackData = CallbackData.GetOrCreate();

            comCallData.pUserDefined = GCHandle.ToIntPtr(callbackData.Handle);

            [UnmanagedCallersOnly]
            static int InvokeCallback(ComCallData* comCallData)
            {
                try
                {
                    CallbackData callbackData = Unsafe.As<CallbackData>(GCHandle.FromIntPtr(comCallData->pUserDefined).Target);

                    callbackData.Callback(callbackData.State);

                    return 0; // S_OK
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }

            Guid iid = IID.IID_ICallbackWithNoReentrancyToApplicationSTA;

            int hresult = (*(IContextCallbackVftbl**)contextCallbackPtr)->ContextCallback_4(
                contextCallbackPtr,
                (IntPtr)(delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                &comCallData,
                &iid,
                /* iMethod */ 5,
                IntPtr.Zero);

            // This call is critical to ensure that the callback data is kept alive until we get here.
            // This prevents its finalizer to run (that finalizer would free the GC handle used in the
            // native callback to get back the target callback data that contains the dispatch parameters).
            GC.KeepAlive(callbackData);

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