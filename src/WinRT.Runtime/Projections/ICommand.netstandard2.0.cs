// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.WinRT.Interop;
using WinRT;
using WinRT.Interop;

namespace ABI.System.Windows.Input
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
#if EMBED
    internal
#else
    public
#endif
    unsafe class ICommand : global::System.Windows.Input.ICommand
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

            private static Delegate[] DelegateCache = new Delegate[4];

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _add_CanExecuteChanged_0 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new ICommand_Delegates.add_CanExecuteChanged_0(Do_Abi_add_CanExecuteChanged_0)),
                    _remove_CanExecuteChanged_1 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new _remove_EventHandler(Do_Abi_remove_CanExecuteChanged_1)),
                    _CanExecute_2 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new ICommand_Delegates.CanExecute_2(Do_Abi_CanExecute_2)),
                    _Execute_3 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new ICommand_Delegates.Execute_3(Do_Abi_Execute_3)),

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }


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

            private volatile static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> _canExecuteChanged_TokenTables;
            private static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> MakeConditionalWeakTable()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _canExecuteChanged_TokenTables, new(), null);
                return _canExecuteChanged_TokenTables;
            }

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> _CanExecuteChanged_TokenTables => _canExecuteChanged_TokenTables ?? MakeConditionalWeakTable();

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

        public static implicit operator ICommand(IObjectReference obj) => (obj != null) ? new ICommand(obj) : null;
        public static implicit operator ICommand(ObjectReference<Vftbl> obj) => (obj != null) ? new ICommand(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public ICommand(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public ICommand(ObjectReference<Vftbl> obj)
        {
            _obj = obj;

            _CanExecuteChanged =
                new EventHandlerEventSource(_obj,
                _obj.Vftbl.add_CanExecuteChanged_0,
                _obj.Vftbl.remove_CanExecuteChanged_1);
        }


        public unsafe bool CanExecute(object parameter)
        {
            ObjectReferenceValue __parameter = default;
            byte __retval = default;
            try
            {
                __parameter = MarshalInspectable<object>.CreateMarshaler2(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CanExecute_2(ThisPtr, MarshalInspectable<object>.GetAbi(__parameter), out __retval));
                GC.KeepAlive(_obj);
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        public unsafe void Execute(object parameter)
        {
            ObjectReferenceValue __parameter = default;
            try
            {
                __parameter = MarshalInspectable<object>.CreateMarshaler2(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Execute_3(ThisPtr, MarshalInspectable<object>.GetAbi(__parameter)));
                GC.KeepAlive(_obj);
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        private readonly EventHandlerEventSource _CanExecuteChanged;

        public event global::System.EventHandler CanExecuteChanged
        {
            add
            {
                _CanExecuteChanged.Subscribe(value);
            }
            remove
            {
                _CanExecuteChanged.Unsubscribe(value);
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
