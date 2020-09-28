using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
    [DynamicInterfaceCastableImplementation]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA")]
    public unsafe interface INotifyDataErrorInfo : global::System.ComponentModel.INotifyDataErrorInfo
    {
        [Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public delegate* unmanaged<IntPtr, byte*, int> get_HasErrors_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> add_ErrorsChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_ErrorsChanged_2;
            public delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int> GetErrors_3;


            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    get_HasErrors_0 = &Do_Abi_get_HasErrors_0,
                    add_ErrorsChanged_1 = &Do_Abi_add_ErrorsChanged_1,
                    remove_ErrorsChanged_2 = &Do_Abi_remove_ErrorsChanged_2,
                    GetErrors_3 = &Do_Abi_GetErrors_3
                };
            }


            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_GetErrors_3(IntPtr thisPtr, IntPtr propertyName, IntPtr* result)
            {
                global::System.Collections.Generic.IEnumerable<object> __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyDataErrorInfo>(thisPtr).GetErrors(MarshalString.FromAbi(propertyName)).OfType<object>();
                    *result = global::ABI.System.Collections.Generic.IEnumerable<object>.FromManaged(__result);

                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }


            [UnmanagedCallersOnly]
            private static unsafe int Do_Abi_get_HasErrors_0(IntPtr thisPtr, byte* value)
            {
                bool __value = default;

                *value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyDataErrorInfo>(thisPtr).HasErrors;
                    *value = (byte)(__value ? 1 : 0);

                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> _ErrorsChanged_TokenTables = new global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>>();

            [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall)})]
            private static unsafe int Do_Abi_add_ErrorsChanged_1(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                *token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyDataErrorInfo>(thisPtr);
                    var __handler = global::ABI.System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>.FromAbi(handler);
                    *token = _ErrorsChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.ErrorsChanged += __handler;
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }

            [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall)})]
            private static unsafe int Do_Abi_remove_ErrorsChanged_2(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyDataErrorInfo>(thisPtr);
                    if (_ErrorsChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
                    {
                        __this.ErrorsChanged -= __handler;
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

        //public static implicit operator INotifyDataErrorInfo(IObjectReference obj) => (obj != null) ? new INotifyDataErrorInfo(obj) : null;
        //protected readonly ObjectReference<Vftbl> _obj;
        //public IObjectReference ObjRef { get => _obj; }
        //public IntPtr ThisPtr => _obj.ThisPtr;
        //public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        //public A As<A>() => _obj.AsType<A>();
        //public INotifyDataErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        //internal INotifyDataErrorInfo(ObjectReference<Vftbl> obj)
        //{
        //    _obj = obj;

        //    _ErrorsChanged =
        //        new EventSource<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>(_obj,
        //        (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)_obj.Vftbl.add_ErrorsChanged_1,
        //        _obj.Vftbl.remove_ErrorsChanged_2);
        //}

        private static EventSource<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>> _ErrorsChanged(IWinRTObject _this)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)_this).GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            return (EventSource<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>)_this.GetOrCreateTypeHelperData(typeof(global::System.Collections.Specialized.INotifyCollectionChanged).TypeHandle,
                () => new EventSource<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>(_obj,
                    (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)_obj.Vftbl.add_ErrorsChanged_1,
                    _obj.Vftbl.remove_ErrorsChanged_2));
        }

        unsafe global::System.Collections.IEnumerable global::System.ComponentModel.INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            MarshalString __propertyName = default;
            IntPtr __retval = default;
            try
            {
                __propertyName = MarshalString.CreateMarshaler(propertyName);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetErrors_3(ThisPtr, MarshalString.GetAbi(__propertyName), &__retval));
                return (global::ABI.System.Collections.Generic.IEnumerable<object>)(object)IInspectable.FromAbi(__retval);
            }
            finally
            {
                MarshalString.DisposeMarshaler(__propertyName);
                global::ABI.System.Collections.Generic.IEnumerable<object>.DisposeAbi(__retval);
            }
        }

        unsafe bool global::System.ComponentModel.INotifyDataErrorInfo.HasErrors
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasErrors_0(ThisPtr, &__retval));
                return __retval != 0;
            }
        }

        event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged
        {
            add => _ErrorsChanged((IWinRTObject)this).Subscribe(value);
            remove => _ErrorsChanged((IWinRTObject)this).Unsubscribe(value);
        }

        //private EventSource<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>> _ErrorsChanged;
    }
    
    internal static class INotifyDataErrorInfo_Delegates
    {
        public unsafe delegate int get_HasErrors_0(IntPtr thisPtr, byte* value);
        public unsafe delegate int add_ErrorsChanged_1(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token);
        public unsafe delegate int remove_ErrorsChanged_2(IntPtr thisPtr, global::WinRT.EventRegistrationToken token);
        public unsafe delegate int GetErrors_3(IntPtr thisPtr, IntPtr propertyName, IntPtr* result);
    }
}
