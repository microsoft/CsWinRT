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
    [Guid("c50898f6-c536-5f47-8583-8b2c2438a13b")]
    internal static class CanExecuteChangedEventHandler
    {
        private delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args);

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static CanExecuteChangedEventHandler()
        {
            AbiInvokeDelegate = (Abi_Invoke)Do_Abi_Invoke;
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(CanExecuteChangedEventHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::System.EventHandler managedDelegate) => ComWrappersSupport.CreateCCWForObject(managedDelegate).As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(CanExecuteChangedEventHandler)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::System.EventHandler<object>>.GetAbi(value);

        public static unsafe global::System.EventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
            return (global::System.EventHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.EventHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private class NativeDelegateWrapper
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
            }

            public void Invoke(object sender, EventArgs args)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
                IObjectReference __sender = default;
                IObjectReference __args = default;
                var __params = new object[] { ThisPtr, null, null };
                try
                {
                    __sender = MarshalInspectable.CreateMarshaler(sender);
                    __params[1] = MarshalInspectable.GetAbi(__sender);
                    __args = MarshalInspectable.CreateMarshaler(args);
                    __params[2] = MarshalInspectable.GetAbi(__args);
                    abiInvoke.DynamicInvokeAbi(__params);
                }
                finally
                {
                    MarshalInspectable.DisposeMarshaler(__sender);
                    MarshalInspectable.DisposeMarshaler(__args);
                }

            }
        }

        public static IntPtr FromManaged(global::System.EventHandler managedDelegate) => CreateMarshaler(managedDelegate).GetRef();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.EventHandler<object>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.EventHandler<object>>.DisposeAbi(abi);

        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args)
        {
            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.Delegate invoke) =>
                {
                    invoke.DynamicInvoke(
                        MarshalInspectable.FromAbi(sender),
                        MarshalInspectable.FromAbi(args) as EventArgs ?? EventArgs.Empty);
                });
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    internal sealed class CanExecuteChangedEventSource : EventSource<global::System.EventHandler>
    {
        internal CanExecuteChangedEventSource(IObjectReference obj, _add_EventHandler addHandler, _remove_EventHandler removeHandler) : base(obj, addHandler, removeHandler)
        {
        }

        protected override IObjectReference CreateMarshaler(EventHandler del)
        {
            return CanExecuteChangedEventHandler.CreateMarshaler(del);
        }

        protected override void DisposeMarshaler(IObjectReference marshaler)
        {
            CanExecuteChangedEventHandler.DisposeMarshaler(marshaler);
        }

        protected override IntPtr GetAbi(IObjectReference marshaler)
        {
            return CanExecuteChangedEventHandler.GetAbi(marshaler);
        }
    }

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

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> _CanExecuteChanged_TokenTables = new ConditionalWeakTable<global::System.Windows.Input.ICommand, EventRegistrationTokenTable<global::System.EventHandler>>();

            private static unsafe int Do_Abi_add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, out global::WinRT.EventRegistrationToken token)
            {
                token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr);
                    var __handler = CanExecuteChangedEventHandler.FromAbi(handler);
                    token = _CanExecuteChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
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
                    if (_CanExecuteChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
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
                new CanExecuteChangedEventSource(_obj,
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

        private readonly EventSource<global::System.EventHandler> _CanExecuteChanged;

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
    public static class ICommand_Delegates
    {
        public unsafe delegate int CanExecute_2(IntPtr thisPtr, IntPtr parameter, out byte result);
        public unsafe delegate int Execute_3(IntPtr thisPtr, IntPtr parameter);
    }
}
