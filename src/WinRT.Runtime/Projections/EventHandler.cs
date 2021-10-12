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

namespace ABI.System
{
    [Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"), EditorBrowsable(EditorBrowsableState.Never)]
    public static class EventHandler<T>
    {
        public static Guid PIID = GuidGenerator.CreateIID(typeof(global::System.EventHandler<T>));
        private static readonly global::System.Type Abi_Invoke_Type = Expression.GetDelegateType(new global::System.Type[] { typeof(void*), typeof(IntPtr), Marshaler<T>.AbiType, typeof(int) });

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static EventHandler()
        {
            AbiInvokeDelegate = global::System.Delegate.CreateDelegate(Abi_Invoke_Type, typeof(EventHandler<T>).GetMethod(nameof(Do_Abi_Invoke), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(new global::System.Type[] { Marshaler<T>.AbiType })
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

        public static unsafe IObjectReference CreateMarshaler(global::System.EventHandler<T> managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, GuidGenerator.GetIID(typeof(EventHandler<T>)));

        public static IntPtr GetAbi(IObjectReference value) =>
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<global::System.EventHandler<T>>.GetAbi(value);

        public static unsafe global::System.EventHandler<T> FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ComWrappersSupport.GetObjectReferenceForInterface(nativeDelegate)?.As<IDelegateVftbl>(GuidGenerator.GetIID(typeof(EventHandler<T>)));
            return abiDelegate is null ? null : (global::System.EventHandler<T>)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.EventHandler<T>(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
#if NETSTANDARD2_0
        private class NativeDelegateWrapper
#else
        private class NativeDelegateWrapper : IWinRTObject
#endif
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
            }

#if !NETSTANDARD2_0
            IObjectReference IWinRTObject.NativeObject => _nativeDelegate;
            bool IWinRTObject.HasUnwrappableNativeObject => true;
            private volatile ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
            private ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
                return _queryInterfaceCache;
            }
            ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();

            private volatile ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
            private ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
                return _additionalTypeData;
            }
            ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();
#endif

            public void Invoke(object sender, T args)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer(_nativeDelegate.Vftbl.Invoke, Abi_Invoke_Type);
                IObjectReference __sender = default;
                object __args = default;
                var __params = new object[] { ThisPtr, null, null };
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler(sender);
                    __params[1] = MarshalInspectable<object>.GetAbi(__sender);
                    __args = Marshaler<T>.CreateMarshaler(args);
                    __params[2] = Marshaler<T>.GetAbi(__args);
                    abiInvoke.DynamicInvokeAbi(__params);
                }
                finally
                {
                    MarshalInspectable<object>.DisposeMarshaler(__sender);
                    Marshaler<T>.DisposeMarshaler(__args);
                }

            }
        }

        public static IntPtr FromManaged(global::System.EventHandler<T> managedDelegate) =>
            CreateMarshaler(managedDelegate)?.GetRef() ?? IntPtr.Zero;

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.EventHandler<T>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.EventHandler<T>>.DisposeAbi(abi);

        private static unsafe int Do_Abi_Invoke<TAbi>(void* thisPtr, IntPtr sender, TAbi args)
        {
            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(new IntPtr(thisPtr), (global::System.Delegate invoke) =>
                {
                    invoke.DynamicInvoke(MarshalInspectable<object>.FromAbi(sender), Marshaler<T>.FromAbi(args));
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

    [Guid("c50898f6-c536-5f47-8583-8b2c2438a13b")]
    internal static class EventHandler
    {
#if NETSTANDARD2_0
        private delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args);
#endif

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe EventHandler()
        {
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
#if NETSTANDARD2_0
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate = (Abi_Invoke)Do_Abi_Invoke)
#else
                Invoke = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)&Do_Abi_Invoke
#endif
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(EventHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

#if NETSTANDARD2_0
        public static global::System.Delegate AbiInvokeDelegate { get; }
#endif

        public static unsafe IObjectReference CreateMarshaler(global::System.EventHandler managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, GuidGenerator.GetIID(typeof(EventHandler)));

        public static IntPtr GetAbi(IObjectReference value) =>
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<global::System.EventHandler<object>>.GetAbi(value);

        public static unsafe global::System.EventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ComWrappersSupport.GetObjectReferenceForInterface(nativeDelegate)?.As<IDelegateVftbl>(GuidGenerator.GetIID(typeof(EventHandler)));
            return abiDelegate is null ? null : (global::System.EventHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.EventHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
#if NETSTANDARD2_0
        private class NativeDelegateWrapper
#else
        private class NativeDelegateWrapper : IWinRTObject
#endif
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
            }

#if !NETSTANDARD2_0
            IObjectReference IWinRTObject.NativeObject => _nativeDelegate;
            bool IWinRTObject.HasUnwrappableNativeObject => true;
            private volatile ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
            private ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
                return _queryInterfaceCache;
            }
            ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();

            private volatile ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
            private ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
                return _additionalTypeData;
            }
            ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();
#endif

            public unsafe void Invoke(object sender, EventArgs args)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
#if NETSTANDARD2_0
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
#else
                var abiInvoke = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)(_nativeDelegate.Vftbl.Invoke);
#endif
                IObjectReference __sender = default;
                IObjectReference __args = default;
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler(sender);
                    __args = MarshalInspectable<EventArgs>.CreateMarshaler(args);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(abiInvoke(
                        ThisPtr,
                        MarshalInspectable<object>.GetAbi(__sender),
                        MarshalInspectable<EventArgs>.GetAbi(__args)));
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

#if !NETSTANDARD2_0
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args)
        {
            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.Delegate invoke) =>
                {
                    invoke.DynamicInvoke(
                        MarshalInspectable<object>.FromAbi(sender),
                        MarshalInspectable<object>.FromAbi(args) as EventArgs ?? EventArgs.Empty);
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

    internal sealed unsafe class EventHandlerEventSource : EventSource<global::System.EventHandler>
    {
        internal EventHandlerEventSource(IObjectReference obj,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::System.IntPtr, out global::WinRT.EventRegistrationToken, int> addHandler,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::WinRT.EventRegistrationToken, int> removeHandler)
            : base(obj, addHandler, removeHandler)
        {
        }

        protected override IObjectReference CreateMarshaler(global::System.EventHandler del) =>
            del is null ? null : EventHandler.CreateMarshaler(del);

        protected override void DisposeMarshaler(IObjectReference marshaler) =>
            EventHandler.DisposeMarshaler(marshaler);

        protected override IntPtr GetAbi(IObjectReference marshaler) =>
            marshaler is null ? IntPtr.Zero : EventHandler.GetAbi(marshaler);

        protected override State CreateEventState() =>
            new EventState(_obj.ThisPtr, _index);

        private sealed class EventState : State
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override Delegate GetEventInvoke()
            {
                global::System.EventHandler handler = (global::System.Object obj, global::System.EventArgs e) =>
                {
                    var localDel = del;
                    if (localDel != null)
                        localDel.Invoke(obj, e);
                };
                return handler;
            }
        }
    }
}
