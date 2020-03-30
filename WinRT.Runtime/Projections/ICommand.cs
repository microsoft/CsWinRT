using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace Windows.Foundation
{
    [global::WinRT.WindowsRuntimeType]
    internal delegate void EventHandler<T>(object sender, T args);
}

namespace ABI.Windows.Foundation
{
    [Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F")]
    internal static class EventHandler<T>
    {
        public static Guid[] PIIDs = GuidGenerator.CreateIIDs(typeof(global::Windows.Foundation.EventHandler<T>));
        private static readonly Type Abi_Invoke_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(IntPtr), Marshaler<T>.AbiType, typeof(int) });

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static EventHandler()
        {
            AbiInvokeDelegate = global::System.Delegate.CreateDelegate(Abi_Invoke_Type, typeof(EventHandler<T>).GetMethod(nameof(Do_Abi_Invoke), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(new Type[] { Marshaler<T>.AbiType })
            );
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(EventHandler<T>), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::Windows.Foundation.EventHandler<T> managedDelegate) => ComWrappersSupport.CreateCCWForObject(managedDelegate).AsAny<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIIDs(typeof(EventHandler<T>)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::Windows.Foundation.EventHandler<T>>.GetAbi(value);

        public static unsafe global::Windows.Foundation.EventHandler<T> FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
            return (global::Windows.Foundation.EventHandler<T>)ComWrappersSupport.TryRegisterObjectForInterface(new global::Windows.Foundation.EventHandler<T>(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private class NativeDelegateWrapper
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
            }

            public void Invoke(object sender, T args)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer(_nativeDelegate.Vftbl.Invoke, Abi_Invoke_Type);
                IObjectReference __sender = default;
                object __args = default;
                var __params = new object[] { ThisPtr, null, null };
                try
                {
                    __sender = MarshalInspectable.CreateMarshaler(sender);
                    __params[1] = MarshalInspectable.GetAbi(__sender);
                    __args = Marshaler<T>.CreateMarshaler(args);
                    __params[2] = Marshaler<T>.GetAbi(__args);
                    abiInvoke.DynamicInvokeAbi(__params);
                }
                finally
                {
                    MarshalInspectable.DisposeMarshaler(__sender);
                    Marshaler<T>.DisposeMarshaler(__args);
                }

            }
        }

        public static IntPtr FromManaged(global::Windows.Foundation.EventHandler<T> managedDelegate) => CreateMarshaler(managedDelegate).GetRef();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::Windows.Foundation.EventHandler<T>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::Windows.Foundation.EventHandler<T>>.DisposeAbi(abi);

        private static unsafe int Do_Abi_Invoke<TAbi>(void* thisPtr, IntPtr sender, TAbi args)
        {


            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(new IntPtr(thisPtr), (global::System.Delegate invoke) =>
                {
                    invoke.DynamicInvoke(MarshalInspectable.FromAbi(sender), Marshaler<T>.FromAbi(args));
                });
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }
}

namespace ABI.System.Windows.Input
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("E5AF3542-CA67-4081-995B-709DD13792DF")]
    internal class ICommand : global::System.Windows.Input.ICommand
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

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand, global::WinRT.EventRegistrationTokenTable<global::Windows.Foundation.EventHandler<object>>> _CanExecuteChanged_TokenTables = new ConditionalWeakTable<global::System.Windows.Input.ICommand, EventRegistrationTokenTable<global::Windows.Foundation.EventHandler<object>>>();

            private static ConditionalWeakTable<global::System.Windows.Input.ICommand,
                ConditionalWeakTable<global::Windows.Foundation.EventHandler<object>, EventHandler>> _winRTToManagedDelegateTables = new ConditionalWeakTable<global::System.Windows.Input.ICommand, ConditionalWeakTable<global::Windows.Foundation.EventHandler<object>, EventHandler>>();
            private static unsafe int Do_Abi_add_CanExecuteChanged_0(IntPtr thisPtr, IntPtr handler, out global::WinRT.EventRegistrationToken token)
            {
                token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Windows.Input.ICommand>(thisPtr);
                    var __handler = global::ABI.Windows.Foundation.EventHandler<object>.FromAbi(handler);
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
                new EventSource<global::Windows.Foundation.EventHandler<object>>(_obj,
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

        private EventSource<global::Windows.Foundation.EventHandler<object>> _CanExecuteChanged;

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

        private readonly ConditionalWeakTable<EventHandler, global::Windows.Foundation.EventHandler<object>> _managedToWinRTDelegateMap = new ConditionalWeakTable<EventHandler, global::Windows.Foundation.EventHandler<object>>();


        private static global::Windows.Foundation.EventHandler<object> CreateWrapperHandler(EventHandler handler)
        {
            // Check whether it is a round-tripping case i.e. the sender is of the type eventArgs,
            // If so we use it else we pass EventArgs.Empty
            return (object sender, object e) =>
            {
                EventArgs eventArgs = e as EventArgs;
                handler(sender, eventArgs ?? EventArgs.Empty);
            };
        }

        private static EventHandler CreateWrapperHandler(global::Windows.Foundation.EventHandler<object> handler)
        {
            return (object sender, EventArgs e) => handler(sender, e);
        }
    }

    internal static class ICommand_Delegates
    {
        public unsafe delegate int CanExecute_2(IntPtr thisPtr, IntPtr parameter, out byte result);
        public unsafe delegate int Execute_3(IntPtr thisPtr, IntPtr parameter);
    }
}
