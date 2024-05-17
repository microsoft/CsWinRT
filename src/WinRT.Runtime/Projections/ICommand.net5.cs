// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.System.ComponentModel;
using ABI.WinRT.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinRT;

namespace ABI.System.Windows.Input
{
#if EMBED
    internal
#else
    public
#endif
    static class ICommandMethods
    {
        public static global::System.Guid IID => global::WinRT.Interop.IID.IID_ICommand;

        public static IntPtr AbiToProjectionVftablePtr => ICommand.Vftbl.AbiToProjectionVftablePtr;

        public static unsafe bool CanExecute(IObjectReference obj, object parameter)
        {
            var ThisPtr = obj.ThisPtr;
            ObjectReferenceValue __parameter = default;
            byte __retval = default;
            try
            {
                __parameter = MarshalInspectable<object>.CreateMarshaler2(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, byte*, int>**)ThisPtr)[8](ThisPtr, MarshalInspectable<object>.GetAbi(__parameter), &__retval));
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        public static unsafe void Execute(IObjectReference obj, object parameter)
        {
            var ThisPtr = obj.ThisPtr;
            ObjectReferenceValue __parameter = default;
            try
            {
                __parameter = MarshalInspectable<object>.CreateMarshaler2(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)ThisPtr)[9](ThisPtr, MarshalInspectable<object>.GetAbi(__parameter)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        private volatile static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, EventHandlerEventSource> _CanExecuteChanged;
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, EventHandlerEventSource> MakeCanExecuteChangedTable()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _CanExecuteChanged, new(), null);
            return _CanExecuteChanged;
        }
        private static global::System.Runtime.CompilerServices.ConditionalWeakTable<object, EventHandlerEventSource> CanExecuteChanged => _CanExecuteChanged ?? MakeCanExecuteChangedTable();

        public static unsafe EventHandlerEventSource Get_CanExecuteChanged2(IObjectReference obj, object thisObj)
        {
            return CanExecuteChanged.GetValue(thisObj, (key) =>
            {
                var ThisPtr = obj.ThisPtr;

                return new EventHandlerEventSource(obj,
                    (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>**)ThisPtr)[6],
                    (*(delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>**)ThisPtr)[7]);
            });
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
    [DynamicInterfaceCastableImplementation]
    internal unsafe interface ICommand : global::System.Windows.Input.ICommand
    {
        [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public struct Vftbl
#pragma warning restore CA2257
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _add_CanExecuteChanged_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> add_CanExecuteChanged_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)_add_CanExecuteChanged_0; set => _add_CanExecuteChanged_0 = value; }
            private void* _remove_CanExecuteChanged_1;
            public delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> remove_CanExecuteChanged_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int>)_remove_CanExecuteChanged_1; set => _remove_CanExecuteChanged_1 = value; }
            private void* _CanExecute_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, byte*, int> CanExecute_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, byte*, int>)_CanExecute_2; set => _CanExecute_2 = value; }
            private void* _Execute_3;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> Execute_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)_Execute_3; set => _Execute_3 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _add_CanExecuteChanged_0 = (delegate* unmanaged<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>)&Do_Abi_add_CanExecuteChanged_0,
                    _remove_CanExecuteChanged_1 = (delegate* unmanaged<IntPtr, global::WinRT.EventRegistrationToken, int>)&Do_Abi_remove_CanExecuteChanged_1,
                    _CanExecute_2 = (delegate* unmanaged<IntPtr, IntPtr, byte*, int>)&Do_Abi_CanExecute_2,
                    _Execute_3 = (delegate* unmanaged<IntPtr, IntPtr, int>)&Do_Abi_Execute_3,

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 4);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
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
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr, global::WinRT.Interop.IID.IID_ICommand);

        private static global::ABI.WinRT.Interop.EventHandlerEventSource _CanExecuteChanged(IWinRTObject _this)
        {
            var _obj = _this.GetObjectReferenceForType(typeof(global::System.Windows.Input.ICommand).TypeHandle);
            return ICommandMethods.Get_CanExecuteChanged2(_obj, _this);
        }

        unsafe bool global::System.Windows.Input.ICommand.CanExecute(object parameter)
        {
            var obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Windows.Input.ICommand).TypeHandle);
            return ICommandMethods.CanExecute(obj, parameter);
        }

        unsafe void global::System.Windows.Input.ICommand.Execute(object parameter)
        {
            var obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Windows.Input.ICommand).TypeHandle);
            ICommandMethods.Execute(obj, parameter);
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
#if EMBED
    internal
#else
    public
#endif
    static class ICommand_Delegates
    {
        public unsafe delegate int add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token);
        public unsafe delegate int CanExecute_2(IntPtr thisPtr, IntPtr parameter, byte* result);
        public unsafe delegate int Execute_3(IntPtr thisPtr, IntPtr parameter);
    }
}
