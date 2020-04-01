using System;
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
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
    public class ICommand : global::System.Windows.Input.ICommand
    {
        [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public _add_EventHandler add_CanExecuteChanged_0;
            public _remove_EventHandler remove_CanExecuteChanged_1;
            public ICommand_Delegates.CanExecute_2 CanExecute_2;
            public ICommand_Delegates.Execute_3 Execute_3;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    add_CanExecuteChanged_0 = Do_Abi_add_CanExecuteChanged_0,
                    remove_CanExecuteChanged_1 = Do_Abi_remove_CanExecuteChanged_1,
                    CanExecute_2 = Do_Abi_CanExecute_2,
                    Execute_3 = Do_Abi_Execute_3
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_CanExecute_2(IntPtr thisPtr, IntPtr parameter, out byte result)
            {
                bool __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr).CanExecute(MarshalInspectable.FromAbi(parameter)); result = (byte)(__result ? 1 : 0);

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
                    global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr).Execute(MarshalInspectable.FromAbi(parameter));
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler<object>>> _CanExecuteChanged_TokenTables = new ConditionalWeakTable<global::System.Windows.Input.ICommand, EventRegistrationTokenTable<global::System.EventHandler<object>>>();

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand,
                ConditionalWeakTable<global::System.EventHandler<object>, EventHandler>> _winRTToManagedDelegateTables = new ConditionalWeakTable<global::System.Windows.Input.ICommand, ConditionalWeakTable<global::System.EventHandler<object>, EventHandler>>();
            private static unsafe int Do_Abi_add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, out global::WinRT.EventRegistrationToken token)
            {
                token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr);
                    var __handler = global::ABI.System.EventHandler<object>.FromAbi(handler);
                    token = _CanExecuteChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    var managedHandler = _winRTToManagedDelegateTables.GetOrCreateValue(__this).GetValue(__handler, CreateWrapperHandler);
                    __this.CanExecuteChanged += managedHandler;
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
                    if (_CanExecuteChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
                    {
                        if (_winRTToManagedDelegateTables.TryGetValue(__this, out var delegateTable) && delegateTable.TryGetValue(__handler, out var managedHandler))
                        {
                            __this.CanExecuteChanged -= managedHandler;
                        }
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
                new EventSource<global::System.EventHandler<object>>(_obj,
                _obj.Vftbl.add_CanExecuteChanged_0,
                _obj.Vftbl.remove_CanExecuteChanged_1);
        }


        public unsafe bool CanExecute(object parameter)
        {
            IObjectReference __parameter = default;
            byte __retval = default;
            try
            {
                __parameter = MarshalInspectable.CreateMarshaler(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CanExecute_2(ThisPtr, MarshalInspectable.GetAbi(__parameter), out __retval));
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__parameter);
            }
        }

        public unsafe void Execute(object parameter)
        {
            IObjectReference __parameter = default;
            try
            {
                __parameter = MarshalInspectable.CreateMarshaler(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Execute_3(ThisPtr, MarshalInspectable.GetAbi(__parameter)));
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__parameter);
            }
        }

        private EventSource<global::System.EventHandler<object>> _CanExecuteChanged;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _CanExecuteChanged.Subscribe(_managedToWinRTDelegateMap.GetValue(value, CreateWrapperHandler));
            }
            remove
            {
                _CanExecuteChanged.Unsubscribe(_managedToWinRTDelegateMap.GetValue(value, CreateWrapperHandler));
            }
        }

        private readonly ConditionalWeakTable<EventHandler, global::System.EventHandler<object>> _managedToWinRTDelegateMap = new ConditionalWeakTable<EventHandler, global::System.EventHandler<object>>();


        private static global::System.EventHandler<object> CreateWrapperHandler(EventHandler handler)
        {
            // Check whether it is a round-tripping case i.e. the sender is of the type eventArgs,
            // If so we use it else we pass EventArgs.Empty
            return (object sender, object e) =>
            {
                EventArgs eventArgs = e as EventArgs;
                handler(sender, eventArgs ?? EventArgs.Empty);
            };
        }

        private static EventHandler CreateWrapperHandler(global::System.EventHandler<object> handler)
        {
            return (object sender, EventArgs e) => handler(sender, e);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ICommand_Delegates
    {
        public unsafe delegate int CanExecute_2(IntPtr thisPtr, IntPtr parameter, out byte result);
        public unsafe delegate int Execute_3(IntPtr thisPtr, IntPtr parameter);
    }
}
