// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.System.Collections.Specialized
{
#if EMBED
    internal
#else
    public
#endif
    static class INotifyCollectionChangedMethods
    {
        private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, NotifyCollectionChangedEventHandlerEventSource> _CollectionChanged;
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, NotifyCollectionChangedEventHandlerEventSource> MakeCollectionChangedTable()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _CollectionChanged, new(), null);
            return _CollectionChanged;
        }
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, NotifyCollectionChangedEventHandlerEventSource> CollectionChanged => _CollectionChanged ?? MakeCollectionChangedTable();

        public static unsafe global::ABI.WinRT.Interop.EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler> Get_CollectionChanged2(IObjectReference obj, object thisObj)
        {
            return CollectionChanged.GetValue(thisObj, (key) =>
            {
                var ThisPtr = obj.ThisPtr;

                return new NotifyCollectionChangedEventHandlerEventSource(obj,
                    (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>**)ThisPtr)[6],
                    (*(delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>**)ThisPtr)[7]);
            });
        }

        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xE1, 0x55, 0x01, 0x53, 0xA5, 0x28, 0x93, 0x56, 0x87, 0xCE, 0x30, 0x72, 0x4D, 0x95, 0xA0, 0x6D }));

        public static IntPtr AbiToProjectionVftablePtr => INotifyCollectionChanged.Vftbl.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
    internal unsafe interface INotifyCollectionChanged : global::System.Collections.Specialized.INotifyCollectionChanged
    {
        [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public struct Vftbl
#pragma warning restore CA2257
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            private delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> _add_CollectionChanged_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> add_CollectionChanged_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)_add_CollectionChanged_0; set => _add_CollectionChanged_0 = (delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)value; }
            private delegate* unmanaged<IntPtr, global::WinRT.EventRegistrationToken, int> _remove_CollectionChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_CollectionChanged_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_CollectionChanged_1; set => _remove_CollectionChanged_1 = (delegate* unmanaged<IntPtr, global::WinRT.EventRegistrationToken, int>)value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _add_CollectionChanged_0 = &Do_Abi_add_CollectionChanged_0,
                    _remove_CollectionChanged_1 = &Do_Abi_remove_CollectionChanged_1,

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 2);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>> _collectionChanged_TokenTables;

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>> MakeConditionalWeakTable()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _collectionChanged_TokenTables, new(), null);
                return _collectionChanged_TokenTables;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>> _CollectionChanged_TokenTables => _collectionChanged_TokenTables ?? MakeConditionalWeakTable();

            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_add_CollectionChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                *token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Specialized.INotifyCollectionChanged>(thisPtr);
                    var __handler = global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler.FromAbi(handler);
                    *token = _CollectionChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.CollectionChanged += __handler;
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }

            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_remove_CollectionChanged_1(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Specialized.INotifyCollectionChanged>(thisPtr);
                    if (__this != null && _CollectionChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
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
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_INotifyCollectionChanged);

        private static global::ABI.WinRT.Interop.EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler> _CollectionChanged(IWinRTObject _this)
        {
            var _obj = _this.GetObjectReferenceForType(typeof(global::System.Collections.Specialized.INotifyCollectionChanged).TypeHandle);
            return INotifyCollectionChangedMethods.Get_CollectionChanged2(_obj, _this);
        }

        event global::System.Collections.Specialized.NotifyCollectionChangedEventHandler global::System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged
        {
            add => _CollectionChanged((IWinRTObject)this).Subscribe(value);
            remove => _CollectionChanged((IWinRTObject)this).Unsubscribe(value);
        }
    }
}
