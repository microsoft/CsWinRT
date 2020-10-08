using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
    [DynamicInterfaceCastableImplementation]
    [Guid("90B17601-B065-586E-83D9-9ADC3A695284")]
    unsafe interface INotifyPropertyChanged : global::System.ComponentModel.INotifyPropertyChanged
    {
        [Guid("90B17601-B065-586E-83D9-9ADC3A695284")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            private delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> _add_PropertyChanged_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int> add_PropertyChanged_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)_add_PropertyChanged_0; set => _add_PropertyChanged_0 = (delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)value; }
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
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyPropertyChanged, global::WinRT.EventRegistrationTokenTable<global::System.ComponentModel.PropertyChangedEventHandler>> _PropertyChanged_TokenTables = new global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyPropertyChanged, global::WinRT.EventRegistrationTokenTable<global::System.ComponentModel.PropertyChangedEventHandler>>();

            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_add_PropertyChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                *token = default;
                try
                {
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
                    if (_PropertyChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
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
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        private static EventSource<global::System.ComponentModel.PropertyChangedEventHandler> _PropertyChanged(IWinRTObject _this)
        {
            var _obj = (ObjectReference<Vftbl>)_this.GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyPropertyChanged).TypeHandle);
            
            return (EventSource<global::System.ComponentModel.PropertyChangedEventHandler>)_this.GetOrCreateTypeHelperData(typeof(global::System.ComponentModel.INotifyPropertyChanged).TypeHandle,
                () => new EventSource<global::System.ComponentModel.PropertyChangedEventHandler>(_obj, _obj.Vftbl.add_PropertyChanged_0, _obj.Vftbl.remove_PropertyChanged_1));
        }

        event global::System.ComponentModel.PropertyChangedEventHandler global::System.ComponentModel.INotifyPropertyChanged.PropertyChanged
        {
            add => _PropertyChanged((IWinRTObject)this).Subscribe(value);
            remove => _PropertyChanged((IWinRTObject)this).Unsubscribe(value);
        }

    }
}
