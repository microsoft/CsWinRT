using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
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
            managedDelegate is null ? null : ComWrappersSupport.CreateCCWForObject(managedDelegate).As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(EventHandler<T>)));

        public static IntPtr GetAbi(IObjectReference value) =>
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<global::System.EventHandler<T>>.GetAbi(value);

        public static unsafe global::System.EventHandler<T> FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
            return (global::System.EventHandler<T>)ComWrappersSupport.TryRegisterObjectForInterface(new global::System.EventHandler<T>(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
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

            public void Invoke(object sender, T args)
            {
#if NETSTANDARD2_0
                var agileReference = _agileReference;
#else
                var agileReference = ((IWinRTObject)this).AdditionalTypeData.TryGetValue(typeof(AgileReference).TypeHandle, out var agileObj) ? 
                    (AgileReference)agileObj : null;
#endif
                using var agileDelegate = agileReference?.Get()?.As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(EventHandler<T>)));
                var delegateToInvoke = agileDelegate ?? _nativeDelegate;
                IntPtr ThisPtr = delegateToInvoke.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer(delegateToInvoke.Vftbl.Invoke, Abi_Invoke_Type);
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
}
