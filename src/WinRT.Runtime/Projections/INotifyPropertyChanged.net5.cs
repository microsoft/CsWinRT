// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
#if EMBED
    internal
#else
    public
#endif
    static class INotifyPropertyChangedMethods
    {
        public static global::System.Guid IID { get; } = new(GetIID());

        private static ReadOnlySpan<byte> GetIID()
            => FeatureSwitches.IsWuxMode
                ? new(new byte[] { 0x9c, 0xd6, 0x75, 0xcf, 0xf4, 0xf2, 0x6b, 0x48, 0xb3, 0x2, 0xbb, 0x4c, 0x9, 0xba, 0xeb, 0xfa })
                : new(new byte[] { 0x1, 0x76, 0xb1, 0x90, 0x65, 0xb0, 0x6e, 0x58, 0x83, 0xd9, 0x9a, 0xdc, 0x3a, 0x69, 0x52, 0x84 });

        public static IntPtr AbiToProjectionVftablePtr => INotifyPropertyChanged.Vftbl.AbiToProjectionVftablePtr;

        private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, PropertyChangedEventSource> _PropertyChanged;
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, PropertyChangedEventSource> MakePropertyChangedTable()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _PropertyChanged, new(), null);
            return _PropertyChanged;
        }
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, PropertyChangedEventSource> PropertyChanged => _PropertyChanged ?? MakePropertyChangedTable();

        public static unsafe global::ABI.WinRT.Interop.EventSource<global::System.ComponentModel.PropertyChangedEventHandler> Get_PropertyChanged2(IObjectReference obj, object thisObj)
        {
            return PropertyChanged.GetValue(thisObj, (key) =>
            {
                var ThisPtr = obj.ThisPtr;

                return new PropertyChangedEventSource(obj,
                    (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>**)ThisPtr)[6],
                    (*(delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>**)ThisPtr)[7]);
            });
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("90B17601-B065-586E-83D9-9ADC3A695284")]
    [WuxMuxProjectedType(wuxIID: "CF75D69C-F2F4-486B-B302-BB4C09BAEBFA", muxIID: "90B17601-B065-586E-83D9-9ADC3A695284")]
    internal unsafe interface INotifyPropertyChanged : global::System.ComponentModel.INotifyPropertyChanged
    {
#pragma warning disable CA2257
        [Guid("90B17601-B065-586E-83D9-9ADC3A695284")]
        [StructLayout(LayoutKind.Sequential)]
        [WuxMuxProjectedType(wuxIID: "CF75D69C-F2F4-486B-B302-BB4C09BAEBFA", muxIID: "90B17601-B065-586E-83D9-9ADC3A695284")]
        public struct Vftbl
#pragma warning restore CA2257
        {

            internal IInspectable.Vftbl IInspectableVftbl;

            private delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> _add_PropertyChanged_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> add_PropertyChanged_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)_add_PropertyChanged_0; set => _add_PropertyChanged_0 = (delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)value; }
            private delegate* unmanaged<IntPtr, global::WinRT.EventRegistrationToken, int> _remove_PropertyChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_PropertyChanged_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_PropertyChanged_1; set => _remove_PropertyChanged_1 = (delegate* unmanaged<IntPtr, global::WinRT.EventRegistrationToken, int>)value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _add_PropertyChanged_0 = &Do_Abi_add_PropertyChanged_0,
                    _remove_PropertyChanged_1 = &Do_Abi_remove_PropertyChanged_1,

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 2);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }
            private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyPropertyChanged, global::WinRT.EventRegistrationTokenTable<global::System.ComponentModel.PropertyChangedEventHandler>> _PropertyChanged_TokenTablesLazy = null;

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyPropertyChanged, global::WinRT.EventRegistrationTokenTable<global::System.ComponentModel.PropertyChangedEventHandler>> MakeConditionalWeakTable()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _PropertyChanged_TokenTablesLazy, new(), null);
                return _PropertyChanged_TokenTablesLazy;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyPropertyChanged, global::WinRT.EventRegistrationTokenTable<global::System.ComponentModel.PropertyChangedEventHandler>> _PropertyChanged_TokenTables => _PropertyChanged_TokenTablesLazy ?? MakeConditionalWeakTable();

            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_add_PropertyChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                *token = default;
                try
                {
                    global::System.Diagnostics.Debugger.Launch();
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyPropertyChanged>(thisPtr);
                    var __handler = global::ABI.System.ComponentModel.PropertyChangedEventHandler.FromAbi(handler);
                    *token = _PropertyChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.PropertyChanged += __handler;
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }

            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_remove_PropertyChanged_1(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyPropertyChanged>(thisPtr);
                    if (__this != null && _PropertyChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
                    {
                        __this.PropertyChanged -= __handler;
                    }
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_INotifyPropertyChanged);

        private static global::ABI.WinRT.Interop.EventSource<global::System.ComponentModel.PropertyChangedEventHandler> _PropertyChanged(IWinRTObject _this)
        {
            var _obj = _this.GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyPropertyChanged).TypeHandle);
            return INotifyPropertyChangedMethods.Get_PropertyChanged2(_obj, _this);
        }

        event global::System.ComponentModel.PropertyChangedEventHandler global::System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        {
            add => _PropertyChanged((IWinRTObject)this).Subscribe(value);
            remove => _PropertyChanged((IWinRTObject)this).Unsubscribe(value);
        }
    }
}
