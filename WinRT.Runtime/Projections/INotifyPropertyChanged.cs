using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("90B17601-B065-586E-83D9-9ADC3A695284")]
    public class INotifyPropertyChanged : global::System.ComponentModel.INotifyPropertyChanged
    {
        [Guid("90B17601-B065-586E-83D9-9ADC3A695284")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public _add_EventHandler add_PropertyChanged_0;
            public _remove_EventHandler remove_PropertyChanged_1;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    add_PropertyChanged_0 = Do_Abi_add_PropertyChanged_0,
                    remove_PropertyChanged_1 = Do_Abi_remove_PropertyChanged_1
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyPropertyChanged, global::WinRT.EventRegistrationTokenTable<global::System.ComponentModel.PropertyChangedEventHandler>> _PropertyChanged_TokenTables;
            private static unsafe int Do_Abi_add_PropertyChanged_0(IntPtr thisPtr, IntPtr handler, out global::WinRT.EventRegistrationToken token)
            {
                token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyPropertyChanged>(thisPtr);
                    var __handler = global::ABI.System.ComponentModel.PropertyChangedEventHandler.FromAbi(handler);
                    token = _PropertyChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.PropertyChanged += __handler;
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }
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

        public static implicit operator INotifyPropertyChanged(IObjectReference obj) => (obj != null) ? new INotifyPropertyChanged(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public INotifyPropertyChanged(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal INotifyPropertyChanged(ObjectReference<Vftbl> obj)
        {
            _obj = obj;

            _PropertyChanged =
                new EventSource<global::System.ComponentModel.PropertyChangedEventHandler>(_obj,
                _obj.Vftbl.add_PropertyChanged_0,
                _obj.Vftbl.remove_PropertyChanged_1);
        }


        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged
        {
            add => _PropertyChanged.Subscribe(value);
            remove => _PropertyChanged.Unsubscribe(value);
        }

        private EventSource<global::System.ComponentModel.PropertyChangedEventHandler> _PropertyChanged;
    }
}
