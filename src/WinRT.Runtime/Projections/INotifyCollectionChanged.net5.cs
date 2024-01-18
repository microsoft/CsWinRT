// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.System.Collections.Specialized
{
#if EMBED
    internal
#else
    public
#endif
    static class INotifyCollectionChangedMethods
    {
        private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, NotifyCollectionChangedEventSource> _CollectionChanged;
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, NotifyCollectionChangedEventSource> MakeCollectionChangedTable()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _CollectionChanged, new(), null);
            return _CollectionChanged;
        }
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, NotifyCollectionChangedEventSource> CollectionChanged => _CollectionChanged ?? MakeCollectionChangedTable();


        public static unsafe (Action<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>, Action<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>) Get_CollectionChanged(IObjectReference obj, object thisObj)
        {
            var eventSource = _CollectionChanged.GetValue(thisObj, (key) =>
            {
                var ThisPtr = obj.ThisPtr;

                return new NotifyCollectionChangedEventSource(obj,
                    (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>**)ThisPtr)[6],
                    (*(delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>**)ThisPtr)[7]);
            });
            return eventSource.EventActions;
        }
        public static global::System.Guid IID { get; } = new(GetIID());

        private static ReadOnlySpan<byte> GetIID()
            => FeatureSwitches.WuxMuxMode switch
            {
                Projections.UiXamlMode.WindowsUiXaml => new(new byte[] { 0xd5, 0x67, 0xb1, 0x28, 0x31, 0x1a, 0x5b, 0x46, 0x9b, 0x25, 0xd5, 0xc3, 0xae, 0x68, 0x6c, 0x40 }),
                Projections.UiXamlMode.MicrosoftUiXaml => new(new byte[] { 0xe1, 0x55, 0x1, 0x53, 0xa5, 0x28, 0x93, 0x56, 0x87, 0xce, 0x30, 0x72, 0x4d, 0x95, 0xa0, 0x6d }),
                _ => throw new InvalidOperationException("Invalid UI XAML mode")
            };
        public static IntPtr AbiToProjectionVftablePtr => INotifyCollectionChanged.Vftbl.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
    [WuxMuxProjectedType(wuxIID: "28b167d5-1a31-465b-9b25-d5c3ae686c40", muxIID: "530155E1-28A5-5693-87CE-30724D95A06D")]
    internal unsafe interface INotifyCollectionChanged : global::System.Collections.Specialized.INotifyCollectionChanged
    {
        [Guid("530155E1-28A5-5693-87CE-30724D95A06D")]
        [WuxMuxProjectedType(wuxIID: "28b167d5-1a31-465b-9b25-d5c3ae686c40", muxIID: "530155E1-28A5-5693-87CE-30724D95A06D")]
        public struct Vftbl
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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
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
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        private static (Action<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>, Action<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>) _CollectionChanged(IWinRTObject _this)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)_this).GetObjectReferenceForType(typeof(global::System.Collections.Specialized.INotifyCollectionChanged).TypeHandle));
            return INotifyCollectionChangedMethods.Get_CollectionChanged(_obj, _this);
        }

        event global::System.Collections.Specialized.NotifyCollectionChangedEventHandler global::System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged
        {
            add => _CollectionChanged((IWinRTObject)this).Item1(value);
            remove => _CollectionChanged((IWinRTObject)this).Item2(value);
        }
    }
}
