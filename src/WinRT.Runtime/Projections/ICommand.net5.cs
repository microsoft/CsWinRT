using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.Windows.Input
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
    [DynamicInterfaceCastableImplementation]
    public unsafe interface ICommand : global::System.Windows.Input.ICommand
    {
        [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _add_CanExecuteChanged_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int> add_CanExecuteChanged_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out global::WinRT.EventRegistrationToken, int>)_add_CanExecuteChanged_0; set => _add_CanExecuteChanged_0 = value; }
            private void* _remove_CanExecuteChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_CanExecuteChanged_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_CanExecuteChanged_1; set => _remove_CanExecuteChanged_1 = value; }
            private void* _CanExecute_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out byte, int> CanExecute_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out byte, int>)_CanExecute_2; set => _CanExecute_2 = value; }
            private void* _Execute_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> Execute_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)_Execute_3; set => _Execute_3 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _add_CanExecuteChanged_0 = (delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*,
                    int>)&Do_Abi_add_CanExecuteChanged_0,
                    _remove_CanExecuteChanged_1 = (delegate* unmanaged<IntPtr, global::WinRT.EventRegistrationToken, int>)&Do_Abi_remove_CanExecuteChanged_1,
                    _CanExecute_2 = (delegate* unmanaged<IntPtr, IntPtr, byte*, int>)&Do_Abi_CanExecute_2,
                    _Execute_3 = (delegate* unmanaged<IntPtr, IntPtr, int>)&Do_Abi_Execute_3,

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }


            [UnmanagedCallersOnly]

            private static unsafe int Do_Abi_CanExecute_2(IntPtr thisPtr, IntPtr parameter, byte* result)
            {
                bool __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr).CanExecute(MarshalInspectable<object>.FromAbi(parameter)); *result = (byte)(__result ? 1 : 0);

                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }


            [UnmanagedCallersOnly]

            private static unsafe int Do_Abi_Execute_3(IntPtr thisPtr, IntPtr parameter)
            {
                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr).Execute(MarshalInspectable<object>.FromAbi(parameter));
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> _canExecuteChanged_TokenTables;
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> MakeConditionalWeakTable()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _canExecuteChanged_TokenTables, new(), null);
                return _canExecuteChanged_TokenTables;
            }
            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> _CanExecuteChanged_TokenTables => _canExecuteChanged_TokenTables ?? MakeConditionalWeakTable();

            [UnmanagedCallersOnly]

            private static unsafe int Do_Abi_add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                *token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr);
                    var __handler = EventHandler.FromAbi(handler);
                    *token = _CanExecuteChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.CanExecuteChanged += __handler;
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }


            [UnmanagedCallersOnly]

            private static unsafe int Do_Abi_remove_CanExecuteChanged_1(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr);
                    if (__this != null && _CanExecuteChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
                    {
                        __this.CanExecuteChanged -= __handler;
                    }
                    return 0;
                }
                catch (global::System.Exception __ex)
                {
                    return __ex.HResult;
                }
            }
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        private static EventHandlerEventSource _CanExecuteChanged(IWinRTObject _this)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)_this).GetObjectReferenceForType(typeof(global::System.Windows.Input.ICommand).TypeHandle));

            return (EventHandlerEventSource)_this.GetOrCreateTypeHelperData(typeof(global::System.Windows.Input.ICommand).TypeHandle,
                () => new EventHandlerEventSource(_obj, _obj.Vftbl.add_CanExecuteChanged_0, _obj.Vftbl.remove_CanExecuteChanged_1));
        }

        unsafe bool global::System.Windows.Input.ICommand.CanExecute(object parameter)
        {
            IObjectReference __parameter = default;
            byte __retval = default;
            try
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Windows.Input.ICommand).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                __parameter = MarshalInspectable<object>.CreateMarshaler(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CanExecute_2(ThisPtr, MarshalInspectable<object>.GetAbi(__parameter), out __retval));
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        unsafe void global::System.Windows.Input.ICommand.Execute(object parameter)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Windows.Input.ICommand).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            IObjectReference __parameter = default;
            try
            {
                __parameter = MarshalInspectable<object>.CreateMarshaler(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Execute_3(ThisPtr, MarshalInspectable<object>.GetAbi(__parameter)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        event global::System.EventHandler global::System.Windows.Input.ICommand.CanExecuteChanged
        {
            add
            {
                _CanExecuteChanged((IWinRTObject)this).Subscribe(value);
            }
            remove
            {
                _CanExecuteChanged((IWinRTObject)this).Unsubscribe(value);
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ICommand_Delegates
    {
        public unsafe delegate int add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token);
        public unsafe delegate int CanExecute_2(IntPtr thisPtr, IntPtr parameter, byte* result);
        public unsafe delegate int Execute_3(IntPtr thisPtr, IntPtr parameter);
    }
}
