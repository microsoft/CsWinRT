// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WinRT.Interop
{   
    internal unsafe sealed class EventHandlerEventSource<T> : EventSource<System.EventHandler<T>>
    {
        internal EventHandlerEventSource(IObjectReference obj,
#if NET
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index) : base(obj, addHandler, removeHandler, index)
        {
        }

        protected override ObjectReferenceValue CreateMarshaler(System.EventHandler<T> del) =>
            ABI.System.EventHandler<T>.CreateMarshaler2(del);

        protected override EventSourceState<System.EventHandler<T>> CreateEventState() =>
            new EventState(_obj.ThisPtr, _index);

        private sealed class EventState : EventSourceState<System.EventHandler<T>>
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override System.EventHandler<T> GetEventInvoke()
            {
                System.EventHandler<T> handler = (System.Object obj, T e) =>
                {
                    var localDel = (System.EventHandler<T>)del;
                    if (localDel != null)
                        localDel.Invoke(obj, e);
                };
                return handler;
            }
        }
    }
}
