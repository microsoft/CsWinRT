using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("E3DE52F6-1E32-5DA6-BB2D-B5B6096C962D")]
    public static class PropertyChangedEventHandler
    {
#if NETSTANDARD2_0
        private unsafe delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e);
#endif

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe PropertyChangedEventHandler()
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
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(PropertyChangedEventHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventHandler managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, GuidGenerator.GetIID(typeof(PropertyChangedEventHandler)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.GetAbi(value);

        public static unsafe global::System.ComponentModel.PropertyChangedEventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ComWrappersSupport.GetObjectReferenceForInterface(nativeDelegate)?.As<IDelegateVftbl>(GuidGenerator.GetIID(typeof(PropertyChangedEventHandler)));
            return abiDelegate is null ? null : (global::System.ComponentModel.PropertyChangedEventHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.ComponentModel.PropertyChangedEventHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
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
            private volatile ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _QueryInterfaceCache = null;
            private ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _QueryInterfaceCache, new ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
                return _QueryInterfaceCache;
            }
            ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _QueryInterfaceCache ?? MakeQueryInterfaceCache();

            private volatile ConcurrentDictionary<RuntimeTypeHandle, object> _AdditionalTypeData = null;
            private ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _AdditionalTypeData, new ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
                return _AdditionalTypeData;
            }
            ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _AdditionalTypeData ?? MakeAdditionalTypeData();
#endif

            public unsafe void Invoke(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
#if NETSTANDARD2_0
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
#else
                var abiInvoke = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)(_nativeDelegate.Vftbl.Invoke);
#endif
                IObjectReference __sender = default;
                IObjectReference __e = default;
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler(sender);
                    __e = global::ABI.System.ComponentModel.PropertyChangedEventArgs.CreateMarshaler(e);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(abiInvoke(ThisPtr, MarshalInspectable<object>.GetAbi(__sender), global::ABI.System.ComponentModel.PropertyChangedEventArgs.GetAbi(__e)));
                }
                finally
                {
                    MarshalInspectable<object>.DisposeMarshaler(__sender);
                    global::ABI.System.ComponentModel.PropertyChangedEventArgs.DisposeMarshaler(__e);
                }

            }
        }

        public static IntPtr FromManaged(global::System.ComponentModel.PropertyChangedEventHandler managedDelegate) =>
            CreateMarshaler(managedDelegate)?.GetRef() ?? IntPtr.Zero;

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeAbi(abi);

#if !NETSTANDARD2_0
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e)
        {
            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.ComponentModel.PropertyChangedEventHandler invoke) =>
                {
                    invoke(MarshalInspectable<object>.FromAbi(sender), global::ABI.System.ComponentModel.PropertyChangedEventArgs.FromAbi(e));
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

    internal sealed unsafe class PropertyChangedEventSource : EventSource<global::System.ComponentModel.PropertyChangedEventHandler>
    {
        internal PropertyChangedEventSource(IObjectReference obj,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::System.IntPtr, out global::WinRT.EventRegistrationToken, int> addHandler,
            delegate* unmanaged[Stdcall]<global::System.IntPtr, global::WinRT.EventRegistrationToken, int> removeHandler)
            : base(obj, addHandler, removeHandler)
        {
        }

        protected override IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventHandler del) =>
            del is null ? null : PropertyChangedEventHandler.CreateMarshaler(del);

        protected override void DisposeMarshaler(IObjectReference marshaler) =>
            PropertyChangedEventHandler.DisposeMarshaler(marshaler);

        protected override IntPtr GetAbi(IObjectReference marshaler) =>
            marshaler is null ? IntPtr.Zero : PropertyChangedEventHandler.GetAbi(marshaler);

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
                global::System.ComponentModel.PropertyChangedEventHandler handler =
                    (global::System.Object obj, global::System.ComponentModel.PropertyChangedEventArgs e) =>
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
