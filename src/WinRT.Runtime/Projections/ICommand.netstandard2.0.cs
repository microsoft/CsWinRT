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

        public static unsafe IObjectReference CreateMarshaler(global::System.EventHandler managedDelegate) =>
            managedDelegate is null ? null : ComWrappersSupport.CreateCCWForObject(managedDelegate).As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(CanExecuteChangedEventHandler)));

        public static IntPtr GetAbi(IObjectReference value) => 
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<global::System.EventHandler<object>>.GetAbi(value);

        public static unsafe global::System.EventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
            return (global::System.EventHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.EventHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private class NativeDelegateWrapper
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;
            private readonly AgileReference _agileReference = default;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
                if (_nativeDelegate.TryAs<ABI.WinRT.Interop.IAgileObject.Vftbl>(out var objRef) < 0)
                {
                    _agileReference = new AgileReference(_nativeDelegate);
                }
                else
                {
                    objRef.Dispose();
                }
            }

            public void Invoke(object sender, EventArgs args)
            {
                using var agileDelegate = _agileReference?.Get()?.As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(CanExecuteChangedEventHandler)));
                var delegateToInvoke = agileDelegate ?? _nativeDelegate;
                IntPtr ThisPtr = delegateToInvoke.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(delegateToInvoke.Vftbl.Invoke);
                IObjectReference __sender = default;
                IObjectReference __args = default;
                var __params = new object[] { ThisPtr, null, null };
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler(sender);
                    __params[1] = MarshalInspectable<object>.GetAbi(__sender);
                    __args = MarshalInspectable<EventArgs>.CreateMarshaler(args);
                    __params[2] = MarshalInspectable<EventArgs>.GetAbi(__args);
                    abiInvoke.DynamicInvokeAbi(__params);
                }
                finally
                {
                    MarshalInspectable<object>.DisposeMarshaler(__sender);
                    MarshalInspectable<EventArgs>.DisposeMarshaler(__args);
                }

            }
        }

        public static IntPtr FromManaged(global::System.EventHandler managedDelegate) =>
            CreateMarshaler(managedDelegate)?.GetRef() ?? IntPtr.Zero;

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.EventHandler<object>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.EventHandler<object>>.DisposeAbi(abi);

        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args)
        {
            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.Delegate invoke) =>
                {
                    invoke.DynamicInvoke(
                        MarshalInspectable<object>.FromAbi(sender),
                        MarshalInspectable<EventArgs>.FromAbi(args) ?? EventArgs.Empty);
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

    internal sealed unsafe class CanExecuteChangedEventSource : EventSource<global::System.EventHandler>
    {
        private global::System.EventHandler handler;

        internal CanExecuteChangedEventSource(IObjectReference obj,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::System.IntPtr, out global::WinRT.EventRegistrationToken, int> addHandler,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::WinRT.EventRegistrationToken, int> removeHandler)
            : base(obj, addHandler, removeHandler)
        {
        }

        protected override IObjectReference CreateMarshaler(EventHandler del) =>
            del is null ? null : CanExecuteChangedEventHandler.CreateMarshaler(del);

        protected override void DisposeMarshaler(IObjectReference marshaler) =>
            CanExecuteChangedEventHandler.DisposeMarshaler(marshaler);

        protected override IntPtr GetAbi(IObjectReference marshaler) =>
            marshaler is null ? IntPtr.Zero : CanExecuteChangedEventHandler.GetAbi(marshaler);

        protected override global::System.Delegate EventInvoke
        {
            // This is synchronized from the base class
            get
            {
                if (handler == null)
                {
                    handler = (global::System.Object obj, global::System.EventArgs e) =>
                    {
                        var localDel = _event;
                        if (localDel != null)
                            localDel.Invoke(obj, e);
                    };
                }
                return handler;
            }
        }
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj)), EditorBrowsable(EditorBrowsableState.Never)]
    [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
    public unsafe class ICommand : global::System.Windows.Input.ICommand
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

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::System.EventHandler>> _CanExecuteChanged_TokenTables = new ConditionalWeakTable<global::System.Windows.Input.ICommand, EventRegistrationTokenTable<global::System.EventHandler>>();


            private static unsafe int Do_Abi_add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token)
            {
                *token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr);
                    var __handler = CanExecuteChangedEventHandler.FromAbi(handler);
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
                __parameter = MarshalInspectable<object>.CreateMarshaler(parameter);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CanExecute_2(ThisPtr, MarshalInspectable<object>.GetAbi(__parameter), out __retval));
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__parameter);
            }
        }

        public unsafe void Execute(object parameter)
        {
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
        public unsafe delegate int add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, global::WinRT.EventRegistrationToken* token);
        public unsafe delegate int CanExecute_2(IntPtr thisPtr, IntPtr parameter, byte* result);
        public unsafe delegate int Execute_3(IntPtr thisPtr, IntPtr parameter);
    }
}
