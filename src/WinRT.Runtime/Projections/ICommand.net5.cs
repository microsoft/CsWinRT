// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.System.ComponentModel;
using ABI.WinRT.Interop;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x42, 0x35, 0xAF, 0xE5, 0x67, 0xCA, 0x81, 0x40, 0x99, 0x5B, 0x70, 0x9D, 0xD1, 0x37, 0x92, 0xDF }));

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
            private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int> _add_CanExecuteChanged_0;
            private delegate* unmanaged[Stdcall]<IntPtr, global::WinRT.EventRegistrationToken, int> _remove_CanExecuteChanged_1;
            private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, byte*, int> _CanExecute_2;
            private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int> _Execute_3;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _add_CanExecuteChanged_0 = &Do_Abi_add_CanExecuteChanged_0,
                    _remove_CanExecuteChanged_1 = &Do_Abi_remove_CanExecuteChanged_1,
                    _CanExecute_2 = &Do_Abi_CanExecute_2,
                    _Execute_3 = &Do_Abi_Execute_3,

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 4);
                *(Vftbl*)nativeVftbl = AbiToProjectionVftable;
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }


            [UnmanagedCallersOnly(CallConvs = new global::System.Type[] { typeof(CallConvStdcall) })]

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


            [UnmanagedCallersOnly(CallConvs = new global::System.Type[] { typeof(CallConvStdcall) })]

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

            [UnmanagedCallersOnly(CallConvs = new global::System.Type[] { typeof(CallConvStdcall) })]

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


            [UnmanagedCallersOnly(CallConvs = new global::System.Type[] { typeof(CallConvStdcall) })]

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
}
