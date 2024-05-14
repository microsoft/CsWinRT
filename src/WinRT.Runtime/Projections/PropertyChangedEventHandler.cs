// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.System.ComponentModel
{
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("E3DE52F6-1E32-5DA6-BB2D-B5B6096C962D")]
#if EMBED
    internal
#else
    public
#endif
    static class PropertyChangedEventHandler
    {
#if !NET
        private unsafe delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e);
#endif

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe PropertyChangedEventHandler()
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
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(PropertyChangedEventHandler), sizeof(global::WinRT.Interop.IDelegateVftbl));
            *(IDelegateVftbl*)nativeVftbl = AbiToProjectionVftable;
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        private static readonly Guid IID = new(0xE3DE52F6, 0x1E32, 0x5DA6, 0xBB, 0x2D, 0xB5, 0xB6, 0x09, 0x6C, 0x96, 0x2D);

        public static unsafe IObjectReference CreateMarshaler(global::System.ComponentModel.PropertyChangedEventHandler managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, IID);

        public static unsafe ObjectReferenceValue CreateMarshaler2(global::System.ComponentModel.PropertyChangedEventHandler managedDelegate) => 
            MarshalDelegate.CreateMarshaler2(managedDelegate, IID);

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.GetAbi(value);

        public static unsafe global::System.ComponentModel.PropertyChangedEventHandler FromAbi(IntPtr nativeDelegate)
        {
            return MarshalDelegate.FromAbi<global::System.ComponentModel.PropertyChangedEventHandler>(nativeDelegate);
        }

        public static global::System.ComponentModel.PropertyChangedEventHandler CreateRcw(IntPtr ptr)
        {
            return new global::System.ComponentModel.PropertyChangedEventHandler(new NativeDelegateWrapper(ComWrappersSupport.GetObjectReferenceForInterface<IDelegateVftbl>(ptr, IID)).Invoke);
        }

#if !NET
        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private sealed class NativeDelegateWrapper
#else
        private sealed class NativeDelegateWrapper : IWinRTObject
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

            public unsafe void Invoke(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
#if !NET
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
#else
                var abiInvoke = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)(_nativeDelegate.Vftbl.Invoke);
#endif
                ObjectReferenceValue __sender = default;
                ObjectReferenceValue __e = default;
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler2(sender);
                    __e = global::ABI.System.ComponentModel.PropertyChangedEventArgs.CreateMarshaler2(e);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(abiInvoke(ThisPtr, MarshalInspectable<object>.GetAbi(__sender), MarshalInspectable<object>.GetAbi(__e)));
                }
                finally
                {
                    MarshalInspectable<object>.DisposeMarshaler(__sender);
                    MarshalInspectable<object>.DisposeMarshaler(__e);
                }
            }
        }

        public static IntPtr FromManaged(global::System.ComponentModel.PropertyChangedEventHandler managedDelegate) => 
            CreateMarshaler2(managedDelegate).Detach();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeAbi(abi);

        public static unsafe MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.MarshalerArray CreateMarshalerArray(global::System.ComponentModel.PropertyChangedEventHandler[] array) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.CreateMarshalerArray2(array, CreateMarshaler2);
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.GetAbiArray(box);
        public static unsafe global::System.ComponentModel.PropertyChangedEventHandler[] FromAbiArray(object box) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.FromAbiArray(box, FromAbi);
        public static void CopyAbiArray(global::System.ComponentModel.PropertyChangedEventHandler[] array, object box) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.CopyAbiArray(array, box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::System.ComponentModel.PropertyChangedEventHandler[] array) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.FromManagedArray(array, FromManaged);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.MarshalerArray array) => MarshalInterfaceHelper<global::System.ComponentModel.PropertyChangedEventHandler>.DisposeMarshalerArray(array);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);

#if NET
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr e)
        {
            try
            {
#if NET
                var invoke = ComWrappersSupport.FindObject<global::System.ComponentModel.PropertyChangedEventHandler>(thisPtr);
                invoke.Invoke(MarshalInspectable<object>.FromAbi(sender), global::ABI.System.ComponentModel.PropertyChangedEventArgs.FromAbi(e));
#else
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.ComponentModel.PropertyChangedEventHandler invoke) =>
                {
                    invoke(MarshalInspectable<object>.FromAbi(sender), global::ABI.System.ComponentModel.PropertyChangedEventArgs.FromAbi(e));
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

    internal sealed unsafe class PropertyChangedEventSource : global::ABI.WinRT.Interop.EventSource<global::System.ComponentModel.PropertyChangedEventHandler>
    {
        internal PropertyChangedEventSource(
            IObjectReference objectReference,
#if NET
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> removeHandler)
            : base(objectReference, addHandler, removeHandler)
        {
        }

        protected override ObjectReferenceValue CreateMarshaler(global::System.ComponentModel.PropertyChangedEventHandler del) =>
            PropertyChangedEventHandler.CreateMarshaler2(del);

        protected override global::ABI.WinRT.Interop.EventSourceState<global::System.ComponentModel.PropertyChangedEventHandler> CreateEventSourceState() =>
            new EventState(ObjectReference.ThisPtr, Index);

        private sealed class EventState : global::ABI.WinRT.Interop.EventSourceState<global::System.ComponentModel.PropertyChangedEventHandler>
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override global::System.ComponentModel.PropertyChangedEventHandler GetEventInvoke()
            {
                return (obj, e) => targetDelegate?.Invoke(obj, e);
            }
        }
    }
}
