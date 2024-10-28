// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using ABI.WinRT.Interop;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA")]
#if EMBED
    internal
#else
    public
#endif
    unsafe class INotifyDataErrorInfo : global::System.ComponentModel.INotifyDataErrorInfo
    {
        [Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
#if !NET
            private void* _get_HasErrors_0;
            public delegate* unmanaged[Stdcall]<IntPtr, byte*, int> get_HasErrors_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)_get_HasErrors_0; set => _get_HasErrors_0 = (void*)value; }
            private void* _add_ErrorsChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> add_ErrorsChanged_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)_add_ErrorsChanged_1; set => _add_ErrorsChanged_1 = (void*)value; }
            private void* _remove_ErrorsChanged_2;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_ErrorsChanged_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_ErrorsChanged_2; set => _remove_ErrorsChanged_2 = (void*)value; }
            private void* _GetErrors_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int> GetErrors_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)_GetErrors_3; set => _GetErrors_3 = (void*)value; }
#else
            public delegate* unmanaged<IntPtr, byte*, int> get_HasErrors_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> add_ErrorsChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_ErrorsChanged_2;
            public delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int> GetErrors_3;
#endif

            public static readonly IntPtr AbiToProjectionVftablePtr;

#if !NET
            private static Delegate[] DelegateCache = new Delegate[4];
#endif

            static unsafe Vftbl()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                (*(Vftbl*)AbiToProjectionVftablePtr) = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
#if !NET
                    _get_HasErrors_0 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new INotifyDataErrorInfo_Delegates.get_HasErrors_0(Do_Abi_get_HasErrors_0)).ToPointer(),
                    _add_ErrorsChanged_1 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new INotifyDataErrorInfo_Delegates.add_ErrorsChanged_1(Do_Abi_add_ErrorsChanged_1)).ToPointer(),
                    _remove_ErrorsChanged_2 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new INotifyDataErrorInfo_Delegates.remove_ErrorsChanged_2(Do_Abi_remove_ErrorsChanged_2)).ToPointer(),
                    _GetErrors_3 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new INotifyDataErrorInfo_Delegates.GetErrors_3(Do_Abi_GetErrors_3)).ToPointer()
#else
                    get_HasErrors_0 = &Do_Abi_get_HasErrors_0,
                    add_ErrorsChanged_1 = &Do_Abi_add_ErrorsChanged_1,
                    remove_ErrorsChanged_2 = &Do_Abi_remove_ErrorsChanged_2,
                    GetErrors_3 = &Do_Abi_GetErrors_3
#endif
                };
            }

#if NET
            [UnmanagedCallersOnly]
#endif
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

#if NET
            [UnmanagedCallersOnly]
#endif
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
            private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> _ErrorsChanged_TokenTablesLazy = null;
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> MakeConditionalWeakTable()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _ErrorsChanged_TokenTablesLazy, new(), null);
                return _ErrorsChanged_TokenTablesLazy;
            }
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> _ErrorsChanged_TokenTables => _ErrorsChanged_TokenTablesLazy ?? MakeConditionalWeakTable();

#if NET
            [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall)})]
#endif
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

#if NET
            [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvStdcall)})]
#endif
            private static unsafe int Do_Abi_remove_ErrorsChanged_2(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.ComponentModel.INotifyDataErrorInfo>(thisPtr);
                    if (__this != null && _ErrorsChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
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

        public static implicit operator INotifyDataErrorInfo(IObjectReference obj) => (obj != null) ? new INotifyDataErrorInfo(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public INotifyDataErrorInfo(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal INotifyDataErrorInfo(ObjectReference<Vftbl> obj)
        {
            _obj = obj;

            _ErrorsChanged =
                new EventHandlerEventSource<global::System.ComponentModel.DataErrorsChangedEventArgs>(_obj,
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int>)_obj.Vftbl.add_ErrorsChanged_1,
                _obj.Vftbl.remove_ErrorsChanged_2,
                0);
        }

        public unsafe global::System.Collections.IEnumerable GetErrors(string propertyName)
        {
            IntPtr __retval = default;
            try
            {
                MarshalString.Pinnable __propertyName = new(propertyName);
                fixed (void* ___propertyName = __propertyName)
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetErrors_3(ThisPtr, MarshalString.GetAbi(ref __propertyName), &__retval));
                    GC.KeepAlive(_obj);
                    return (global::ABI.System.Collections.Generic.IEnumerable<object>)(object)IInspectable.FromAbi(__retval);
                }
            }
            finally
            {
                global::ABI.System.Collections.Generic.IEnumerable<object>.DisposeAbi(__retval);
            }
        }

        public unsafe bool HasErrors
        {
            get
            {
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasErrors_0(ThisPtr, &__retval));
                GC.KeepAlive(_obj);
                return __retval != 0;
            }
        }

        public event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged
        {
            add => _ErrorsChanged.Subscribe(value);
            remove => _ErrorsChanged.Unsubscribe(value);
        }

        private EventHandlerEventSource<global::System.ComponentModel.DataErrorsChangedEventArgs> _ErrorsChanged;
    }
    
    internal static class INotifyDataErrorInfo_Delegates
    {
        public unsafe delegate int get_HasErrors_0(IntPtr thisPtr, byte* value);
        public unsafe delegate int add_ErrorsChanged_1(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token);
        public unsafe delegate int remove_ErrorsChanged_2(IntPtr thisPtr, global::WinRT.EventRegistrationToken token);
        public unsafe delegate int GetErrors_3(IntPtr thisPtr, IntPtr propertyName, IntPtr* result);
    }
}
