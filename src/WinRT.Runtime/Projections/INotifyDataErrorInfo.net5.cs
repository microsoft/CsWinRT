﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.System.ComponentModel
{
#if EMBED
    internal
#else
    public
#endif
    static class INotifyDataErrorInfoMethods
    {
        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xCC, 0xC2, 0xE6, 0x0E, 0x3E, 0x27, 0x7D, 0x56, 0xBC, 0x0A, 0x1D, 0xD8, 0x7E, 0xE5, 0x1E, 0xBA }));

        public static IntPtr AbiToProjectionVftablePtr => INotifyDataErrorInfo.Vftbl.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA")]
    internal unsafe interface INotifyDataErrorInfo : global::System.ComponentModel.INotifyDataErrorInfo
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
            
            private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> _ErrorsChanged_TokenTablesLazy = null;
            
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> MakeConditionalWeakTable()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _ErrorsChanged_TokenTablesLazy, new(), null);
                return _ErrorsChanged_TokenTablesLazy;
            }
            
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>>> _ErrorsChanged_TokenTables => _ErrorsChanged_TokenTablesLazy ?? MakeConditionalWeakTable();

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

        private static EventSource__EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> _ErrorsChanged(IWinRTObject _this)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)_this).GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            return (EventSource__EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>)_this.GetOrCreateTypeHelperData(typeof(global::System.Collections.Specialized.INotifyCollectionChanged).TypeHandle,
                () => new EventSource__EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>(_obj,
                    _obj.Vftbl.add_ErrorsChanged_1,
                    _obj.Vftbl.remove_ErrorsChanged_2,
                    0));
        }

        unsafe global::System.Collections.IEnumerable global::System.ComponentModel.INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle));
            IntPtr __retval = default;
            bool success = false;
            try
            {
                _obj.DangerousAddRef(ref success);
                var thisPtr = _obj.DangerousGetPtr();

                MarshalString.Pinnable __propertyName = new(propertyName);
                fixed (void* ___propertyName = __propertyName)
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetErrors_3(thisPtr, MarshalString.GetAbi(ref __propertyName), &__retval));
                    return (global::ABI.System.Collections.Generic.IEnumerable<object>)(object)IInspectable.FromAbi(__retval);
                }
            }
            finally
            {
                global::ABI.System.Collections.Generic.IEnumerable<object>.DisposeAbi(__retval);
                if (success)
                {
                    _obj.DangerousRelease();
                }
            }
        }

        unsafe bool global::System.ComponentModel.INotifyDataErrorInfo.HasErrors
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle));

                bool success = false;
                try
                {
                    _obj.DangerousAddRef(ref success);
                    var thisPtr = _obj.DangerousGetPtr();
                    byte __retval = default;
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasErrors_0(thisPtr, &__retval));
                    return __retval != 0;
                }
                finally
                {
                    if (success)
                    {
                        _obj.DangerousRelease();
                    }
                }
            }
        }

        event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged
        {
            add => _ErrorsChanged((IWinRTObject)this).Subscribe(value);
            remove => _ErrorsChanged((IWinRTObject)this).Unsubscribe(value);
        }
    }
    
    internal static class INotifyDataErrorInfo_Delegates
    {
        public unsafe delegate int get_HasErrors_0(IntPtr thisPtr, byte* value);
        public unsafe delegate int add_ErrorsChanged_1(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token);
        public unsafe delegate int remove_ErrorsChanged_2(IntPtr thisPtr, global::WinRT.EventRegistrationToken token);
        public unsafe delegate int GetErrors_3(IntPtr thisPtr, IntPtr propertyName, IntPtr* result);
    }
}
