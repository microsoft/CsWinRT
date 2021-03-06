using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private unsafe delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e);

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static PropertyChangedEventHandler()
        {
            AbiInvokeDelegate = new Abi_Invoke(Do_Abi_Invoke);
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(PropertyChangedEventHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventHandler managedDelegate) =>
            managedDelegate is null ? null : ComWrappersSupport.CreateCCWForObject(managedDelegate).As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(PropertyChangedEventHandler)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.GetAbi(value);

        public static unsafe global::System.ComponentModel.PropertyChangedEventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
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
#if NETSTANDARD2_0
            private readonly AgileReference _agileReference = default;
#endif

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
                if (_nativeDelegate.TryAs<ABI.WinRT.Interop.IAgileObject.Vftbl>(out var objRef) < 0)
                {
                    var agileReference = new AgileReference(_nativeDelegate);
#if NETSTANDARD2_0
                    _agileReference = agileReference;
#else
                    ((IWinRTObject)this).AdditionalTypeData.TryAdd(typeof(AgileReference).TypeHandle, agileReference);
#endif
                }
                else
                {
                    objRef.Dispose();
                }
            }

#if !NETSTANDARD2_0
            IObjectReference IWinRTObject.NativeObject => _nativeDelegate;
            bool IWinRTObject.HasUnwrappableNativeObject => true;
            ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache { get; } = new();
            ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData { get; } = new();
#endif

            public void Invoke(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
            {
#if NETSTANDARD2_0
                var agileReference = _agileReference;
#else
                var agileReference = ((IWinRTObject)this).AdditionalTypeData.TryGetValue(typeof(AgileReference).TypeHandle, out var agileObj) ?
                    (AgileReference)agileObj : null;
#endif
                using var agileDelegate = agileReference?.Get()?.As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(PropertyChangedEventHandler)));
                var delegateToInvoke = agileDelegate ?? _nativeDelegate;
                IntPtr ThisPtr = delegateToInvoke.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(delegateToInvoke.Vftbl.Invoke);
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
            managedDelegate is null ? IntPtr.Zero : CreateMarshaler(managedDelegate).GetRef();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeAbi(abi);

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
        private global::System.ComponentModel.PropertyChangedEventHandler handler;

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

        protected override global::System.Delegate EventInvoke
        {
            // This is synchronized from the base class
            get
            {
                if (handler == null)
                {
                    handler = (global::System.Object obj, global::System.ComponentModel.PropertyChangedEventArgs e) =>
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
}
