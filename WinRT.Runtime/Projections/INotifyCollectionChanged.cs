﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.Collections.Specialized
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
    public unsafe class INotifyCollectionChanged : global::System.Collections.Specialized.INotifyCollectionChanged
    {
        [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
#if NETSTANDARD2_0
            private void* _add_CollectionChanged_0;
            public delegate* stdcall<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int> add_CollectionChanged_0 { get => (delegate* stdcall<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)_add_CollectionChanged_0; set => _add_CollectionChanged_0=(void*)value; }
            private void* _remove_CollectionChanged_1;
            public delegate* stdcall<IntPtr, global::WinRT.EventRegistrationToken, int> remove_CollectionChanged_1 { get => (delegate* stdcall<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_CollectionChanged_1; set => _remove_CollectionChanged_1=(void*)value; }
#else
            private delegate*<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int> _add_CollectionChanged_0;
            public delegate* stdcall<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int> add_CollectionChanged_0 { get => (delegate* stdcall<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)_add_CollectionChanged_0; set => _add_CollectionChanged_0=(delegate*<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)value; }
            private delegate*<IntPtr, global::WinRT.EventRegistrationToken, int> _remove_CollectionChanged_1;
            public delegate* stdcall<IntPtr, global::WinRT.EventRegistrationToken, int> remove_CollectionChanged_1 { get => (delegate* stdcall<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_CollectionChanged_1; set => _remove_CollectionChanged_1=(delegate*<IntPtr, global::WinRT.EventRegistrationToken, int>)value; }
#endif

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
#if NETSTANDARD2_0
            private static Delegate[] DelegateCache = new Delegate[2];
#endif
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if NETSTANDARD2_0
                    _add_CollectionChanged_0 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new _add_EventHandler(Do_Abi_add_CollectionChanged_0)),
                    _remove_CollectionChanged_1 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new _remove_EventHandler(Do_Abi_remove_CollectionChanged_1)),
#else
                    _add_CollectionChanged_0 = &Do_Abi_add_CollectionChanged_0,
                    _remove_CollectionChanged_1 = &Do_Abi_remove_CollectionChanged_1,
#endif
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>> _CollectionChanged_TokenTables = new global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>>();
            private static unsafe int Do_Abi_add_CollectionChanged_0(IntPtr thisPtr, IntPtr handler, out global::WinRT.EventRegistrationToken token)
            {
                token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Specialized.INotifyCollectionChanged>(thisPtr);
                    var __handler = global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.FromAbi(handler);
                    token = _CollectionChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.CollectionChanged += __handler;
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }
            private static unsafe int Do_Abi_remove_CollectionChanged_1(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Specialized.INotifyCollectionChanged>(thisPtr);
                    if (_CollectionChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
                    {
                        __this.CollectionChanged -= __handler;
                    }
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator INotifyCollectionChanged(IObjectReference obj) => (obj != null) ? new INotifyCollectionChanged(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public INotifyCollectionChanged(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal INotifyCollectionChanged(ObjectReference<Vftbl> obj)
        {
            _obj = obj;

            _CollectionChanged =
                new EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>(_obj,
                _obj.Vftbl.add_CollectionChanged_0,
                _obj.Vftbl.remove_CollectionChanged_1);
        }

        public event global::System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _CollectionChanged.Subscribe(value);
            remove => _CollectionChanged.Unsubscribe(value);
        }

        private EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler> _CollectionChanged;
    }
}
