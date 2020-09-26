using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.Collections.Specialized
{
    [DynamicInterfaceCastableImplementation]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
    public unsafe interface INotifyCollectionChanged : global::System.Collections.Specialized.INotifyCollectionChanged
    {
        [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            private delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> _add_CollectionChanged_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int> add_CollectionChanged_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)_add_CollectionChanged_0; set => _add_CollectionChanged_0 = (delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)value; }
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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>> _CollectionChanged_TokenTables = new global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Collections.Specialized.INotifyCollectionChanged, global::WinRT.EventRegistrationTokenTable<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>>();
            
            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_add_CollectionChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                token = default;
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

        private static EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler> _CollectionChanged(IWinRTObject _this)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)_this).GetObjectReferenceForType(typeof(global::System.Collections.Specialized.INotifyCollectionChanged).TypeHandle));
            
            return (EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>)_this.GetOrCreateTypeHelperData(typeof(global::System.Collections.Specialized.INotifyCollectionChanged).TypeHandle,
                () => new EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>(_obj, _obj.Vftbl.add_CollectionChanged_0, _obj.Vftbl.remove_CollectionChanged_1));
        }

        event global::System.Collections.Specialized.NotifyCollectionChangedEventHandler global::System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged
        {
            add => _CollectionChanged((IWinRTObject)this).Subscribe(value);
            remove => _CollectionChanged((IWinRTObject)this).Unsubscribe(value);
        }

    }
}
