using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.Collections.Specialized
{

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("8B0909DC-2005-5D93-BF8A-725F017BAA8D")]
#if EMBED
    internal
#else
    public
#endif
    static class NotifyCollectionChangedEventHandler
    {
#if !NET
        private unsafe delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e);
#endif

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe NotifyCollectionChangedEventHandler()
        {
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
#if !NET
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate = (Abi_Invoke)Do_Abi_Invoke)
#else
                Invoke = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)&Do_Abi_Invoke
#endif
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(NotifyCollectionChangedEventHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, GuidGenerator.GetIID(typeof(NotifyCollectionChangedEventHandler)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>.GetAbi(value);

        public static unsafe global::System.Collections.Specialized.NotifyCollectionChangedEventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ComWrappersSupport.GetObjectReferenceForInterface<IDelegateVftbl>(nativeDelegate);
            return abiDelegate is null ? null : (global::System.Collections.Specialized.NotifyCollectionChangedEventHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.Collections.Specialized.NotifyCollectionChangedEventHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
#if !NET
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

#if NET
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

            public unsafe void Invoke(object sender, global::System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
#if !NET
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
#else
                var abiInvoke = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)(_nativeDelegate.Vftbl.Invoke);
#endif
                IObjectReference __sender = default;
                IObjectReference __e = default;
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler(sender);
                    __e = global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs.CreateMarshaler(e);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(abiInvoke(ThisPtr, MarshalInspectable<object>.GetAbi(__sender), global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs.GetAbi(__e)));
                }
                finally
                {
                    MarshalInspectable<object>.DisposeMarshaler(__sender);
                    global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs.DisposeMarshaler(__e);
                }

            }
        }

        public static IntPtr FromManaged(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler managedDelegate) =>
            CreateMarshaler(managedDelegate)?.GetRef() ?? IntPtr.Zero;

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>.DisposeAbi(abi);

#if NET
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e)
        {
            try
            {
#if NET
                var invoke = ComWrappersSupport.FindObject<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>(thisPtr);
                invoke.Invoke(MarshalInspectable<object>.FromAbi(sender), global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs.FromAbi(e));
#else
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.Collections.Specialized.NotifyCollectionChangedEventHandler invoke) =>
                {
                    invoke(MarshalInspectable<object>.FromAbi(sender), global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs.FromAbi(e));
                });
#endif
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    internal sealed unsafe class NotifyCollectionChangedEventSource : EventSource<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>
    {
        internal NotifyCollectionChangedEventSource(IObjectReference obj,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::System.IntPtr, out global::WinRT.EventRegistrationToken, int> addHandler,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::WinRT.EventRegistrationToken, int> removeHandler)
            : base(obj, addHandler, removeHandler)
        {
        }

        protected override IObjectReference CreateMarshaler(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler del) =>
            del is null ? null : NotifyCollectionChangedEventHandler.CreateMarshaler(del);

        protected override void DisposeMarshaler(IObjectReference marshaler) =>
            NotifyCollectionChangedEventHandler.DisposeMarshaler(marshaler);

        protected override IntPtr GetAbi(IObjectReference marshaler) =>
            marshaler is null ? IntPtr.Zero : NotifyCollectionChangedEventHandler.GetAbi(marshaler);

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
                global::System.Collections.Specialized.NotifyCollectionChangedEventHandler handler =
                    (global::System.Object obj, global::System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
                    {
                        var localDel = (global::System.Collections.Specialized.NotifyCollectionChangedEventHandler) del;
                        if (localDel != null)
                            localDel.Invoke(obj, e);
                    };
                return handler;
            }
        }
    }
}
