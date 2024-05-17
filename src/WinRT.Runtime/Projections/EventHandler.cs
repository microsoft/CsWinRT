// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT;
using WinRT.Interop;

namespace ABI.System
{
    internal static class EventHandlerMethods<T>
    {
        internal unsafe volatile static delegate*<IObjectReference, object, T, void> _Invoke;

        private static IntPtr abiToProjectionVftablePtr;
        internal static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            bool success = global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
            if (success)
            {
                EventHandler<T>.AbiToProjectionVftablePtr = abiToProjectionVftablePtr;

#if NET
                ComWrappersSupport.RegisterComInterfaceEntries(
                    typeof(global::System.EventHandler<T>),
                    DelegateTypeDetails<global::System.EventHandler<T>>.GetExposedInterfaces(
                        new ComWrappers.ComInterfaceEntry
                        {
                            IID = EventHandler<T>.PIID,
                            Vtable = abiToProjectionVftablePtr
                        }));
#endif
            }
            return success;
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class EventHandlerMethods<T, TAbi> where TAbi : unmanaged
    {
        public unsafe static bool InitRcwHelper(delegate*<IObjectReference, object, T, void> invoke)
        {
            if (EventHandlerMethods<T>._Invoke == null)
            {
                EventHandlerMethods<T>._Invoke = invoke;
            }
            return true;
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, TAbi, int> invoke)
        {
            if (EventHandlerMethods<T>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

#if NET
            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IUnknownVftbl) + sizeof(IntPtr)));
#else
            var abiToProjectionVftablePtr = Marshal.AllocCoTaskMem((sizeof(IUnknownVftbl) + sizeof(IntPtr)));
#endif
            *(IUnknownVftbl*)abiToProjectionVftablePtr = IUnknownVftbl.AbiToProjectionVftbl;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, TAbi, int>*)abiToProjectionVftablePtr)[3] = invoke;

            if (!EventHandlerMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
#if NET
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
#else
                Marshal.FreeCoTaskMem(abiToProjectionVftablePtr);
#endif
                return false;
            }

            return true;
        }

        public static void Abi_Invoke(IntPtr thisPtr, object sender, T args)
        {
#if NET
            var invoke = ComWrappersSupport.FindObject<global::System.EventHandler<T>>(thisPtr);
            invoke.Invoke(sender, args);
#else
            global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.EventHandler<T> invoke) =>
            {
                invoke.Invoke(sender, args);
            });
#endif
        }
    }

    [Guid("9DE1C535-6AE1-11E0-84E1-18A905BCC53F"), EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    static class EventHandler<T>
    {
        public static Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(global::System.EventHandler<T>));

        public static Guid IID => PIID;

        /// <summary>
        /// The ABI delegate type for the fallback, non-AOT scenario.
        /// This is lazily-initialized from the fallback paths below.
        /// </summary>
        private static global::System.Type _abi_invoke_type;

        public static IntPtr AbiToProjectionVftablePtr;

        static unsafe EventHandler()
        {
            ComWrappersSupport.RegisterHelperType(typeof(global::System.EventHandler<T>), typeof(global::ABI.System.EventHandler<T>));
            ComWrappersSupport.RegisterDelegateFactory(typeof(global::System.EventHandler<T>), CreateRcw);

#if NET
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                // On NAOT, we always just use the available vtable, no matter if it's been set or not.
                // See: https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
                // We specifically need this check to be separate than that of the vtable not being null.
                AbiToProjectionVftablePtr = EventHandlerMethods<T>.AbiToProjectionVftablePtr;

                return;
            }
#endif
            
            if (EventHandlerMethods<T>.AbiToProjectionVftablePtr != default)
            {
                AbiToProjectionVftablePtr = EventHandlerMethods<T>.AbiToProjectionVftablePtr;
            }
            else
            {
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                // Initialize the ABI invoke delegate type (we don't want to do that from a method in this type, or it will get rooted).
                // That is because there's other reflection paths just preserving members from EventHandler<T> unconditionally.
                _abi_invoke_type = Projections.GetAbiDelegateType(typeof(void*), typeof(IntPtr), Marshaler<T>.AbiType, typeof(int));

                // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                AbiInvokeDelegate = global::System.Delegate.CreateDelegate(_abi_invoke_type, typeof(EventHandler<T>).GetMethod(nameof(Do_Abi_Invoke), BindingFlags.Static | BindingFlags.NonPublic).
                    MakeGenericMethod(new global::System.Type[] { Marshaler<T>.AbiType }));
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(EventHandler<T>), sizeof(global::WinRT.Interop.IDelegateVftbl));
                *(global::WinRT.Interop.IUnknownVftbl*)AbiToProjectionVftablePtr = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl;
                ((IntPtr*)AbiToProjectionVftablePtr)[3] = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate);
#pragma warning restore IL3050
            }
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::System.EventHandler<T> managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, PIID);

        public static unsafe ObjectReferenceValue CreateMarshaler2(global::System.EventHandler<T> managedDelegate) => 
            MarshalDelegate.CreateMarshaler2(managedDelegate, PIID);

        public static IntPtr GetAbi(IObjectReference value) =>
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<global::System.EventHandler<T>>.GetAbi(value);

        public static unsafe global::System.EventHandler<T> FromAbi(IntPtr nativeDelegate)
        {
            return MarshalDelegate.FromAbi<global::System.EventHandler<T>>(nativeDelegate);
        }

        public static global::System.EventHandler<T> CreateRcw(IntPtr ptr)
        {
            return new global::System.EventHandler<T>(new NativeDelegateWrapper(ComWrappersSupport.GetObjectReferenceForInterface<IDelegateVftbl>(ptr, PIID)).Invoke);
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

            public unsafe void Invoke(object sender, T args)
            {
#if NET
                // Standalone path for NAOT to ensure the linker trims code as expected
                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    EventHandlerMethods<T>._Invoke(_nativeDelegate, sender, args);

                    return;
                }
#endif

                if (EventHandlerMethods<T>._Invoke != null)
                {
                    EventHandlerMethods<T>._Invoke(_nativeDelegate, sender, args);
                }
                else
                {
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                    // Same as in the static constructor, we initialize the ABI delegate type manually here if needed.
                    // We gate this behind a null check to avoid unnecessarily calling Projections.GetAbiDelegateType.
                    if (Volatile.Read(ref _abi_invoke_type) is null)
                    {
                        global::System.Threading.Interlocked.CompareExchange(ref _abi_invoke_type, Projections.GetAbiDelegateType(typeof(void*), typeof(IntPtr), Marshaler<T>.AbiType, typeof(int)), null);
                    }

                    IntPtr ThisPtr = _nativeDelegate.ThisPtr;
                    var abiInvoke = Marshal.GetDelegateForFunctionPointer(_nativeDelegate.Vftbl.Invoke, _abi_invoke_type);
#pragma warning restore IL3050
                    ObjectReferenceValue __sender = default;
                    object __args = default;
                    var __params = new object[] { ThisPtr, null, null };
                    try
                    {
                        __sender = MarshalInspectable<object>.CreateMarshaler2(sender);
                        __params[1] = MarshalInspectable<object>.GetAbi(__sender);
                        __args = Marshaler<T>.CreateMarshaler2(args);
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
        }

        public static IntPtr FromManaged(global::System.EventHandler<T> managedDelegate) => 
            CreateMarshaler2(managedDelegate).Detach();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.EventHandler<T>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.EventHandler<T>>.DisposeAbi(abi);

        public static unsafe MarshalInterfaceHelper<global::System.EventHandler<T>>.MarshalerArray CreateMarshalerArray(global::System.EventHandler<T>[] array) => MarshalInterfaceHelper<global::System.EventHandler<T>>.CreateMarshalerArray2(array, CreateMarshaler2);
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.EventHandler<T>>.GetAbiArray(box);
        public static unsafe global::System.EventHandler<T>[] FromAbiArray(object box) => MarshalInterfaceHelper<global::System.EventHandler<T>>.FromAbiArray(box, FromAbi);
        public static void CopyAbiArray(global::System.EventHandler<T>[] array, object box) => MarshalInterfaceHelper<global::System.EventHandler<T>>.CopyAbiArray(array, box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::System.EventHandler<T>[] array) => MarshalInterfaceHelper<global::System.EventHandler<T>>.FromManagedArray(array, FromManaged);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::System.EventHandler<T>>.MarshalerArray array) => MarshalInterfaceHelper<global::System.EventHandler<T>>.DisposeMarshalerArray(array);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);

        private static unsafe int Do_Abi_Invoke<TAbi>(void* thisPtr, IntPtr sender, TAbi args)
        {
            try
            {
#if NET
                var invoke = ComWrappersSupport.FindObject<global::System.EventHandler<T>>(new IntPtr(thisPtr));
                invoke.Invoke(MarshalInspectable<object>.FromAbi(sender), Marshaler<T>.FromAbi(args));
#else
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(new IntPtr(thisPtr), (global::System.EventHandler<T> invoke) =>
                {
                    invoke.Invoke(MarshalInspectable<object>.FromAbi(sender), Marshaler<T>.FromAbi(args));
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

    [Guid("c50898f6-c536-5f47-8583-8b2c2438a13b")]
    internal static class EventHandler
    {
#if !NET
        private delegate int Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args);
#endif

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe EventHandler()
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
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(EventHandler), sizeof(global::WinRT.Interop.IDelegateVftbl));
            *(global::WinRT.Interop.IDelegateVftbl*)nativeVftbl = AbiToProjectionVftable;
            AbiToProjectionVftablePtr = nativeVftbl;
            ComWrappersSupport.RegisterDelegateFactory(typeof(global::System.EventHandler), CreateRcw);
        }

#if !NET
        public static global::System.Delegate AbiInvokeDelegate { get; }
#endif

        public static Guid IID => global::WinRT.Interop.IID.IID_EventHandler;


        public static unsafe IObjectReference CreateMarshaler(global::System.EventHandler managedDelegate) =>
            managedDelegate is null ? null : MarshalDelegate.CreateMarshaler(managedDelegate, IID);

        public static unsafe ObjectReferenceValue CreateMarshaler2(global::System.EventHandler managedDelegate) =>
            MarshalDelegate.CreateMarshaler2(managedDelegate, IID);

        public static IntPtr GetAbi(IObjectReference value) =>
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<global::System.EventHandler<object>>.GetAbi(value);

        public static unsafe global::System.EventHandler FromAbi(IntPtr nativeDelegate)
        {
            return MarshalDelegate.FromAbi<global::System.EventHandler>(nativeDelegate);
        }

        public static global::System.EventHandler CreateRcw(IntPtr ptr)
        {
            return new global::System.EventHandler(new NativeDelegateWrapper(
#if NET
                ComWrappersSupport.GetObjectReferenceForInterface<IUnknownVftbl>(ptr, IID)).Invoke
#else
                ComWrappersSupport.GetObjectReferenceForInterface<IDelegateVftbl>(ptr, IID)).Invoke
#endif
                );
        }

#if !NET
        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private sealed class NativeDelegateWrapper
#else
        private sealed class NativeDelegateWrapper : IWinRTObject
#endif
        {
#if NET
            private readonly ObjectReference<global::WinRT.Interop.IUnknownVftbl> _nativeDelegate;
#else
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;
#endif

            public NativeDelegateWrapper(
#if NET
                ObjectReference<global::WinRT.Interop.IUnknownVftbl> nativeDelegate
#else
                ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate
#endif
                )
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

            public unsafe void Invoke(object sender, EventArgs args)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
#if !NET
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
#else
                var abiInvoke = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, int>)(*(void***)ThisPtr)[3];
#endif
                ObjectReferenceValue __sender = default;
                ObjectReferenceValue __args = default;
                try
                {
                    __sender = MarshalInspectable<object>.CreateMarshaler2(sender);
                    __args = MarshalInspectable<EventArgs>.CreateMarshaler2(args);
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
            CreateMarshaler2(managedDelegate).Detach();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::System.EventHandler<object>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::System.EventHandler<object>>.DisposeAbi(abi);

        public static unsafe MarshalInterfaceHelper<global::System.EventHandler>.MarshalerArray CreateMarshalerArray(global::System.EventHandler[] array) => MarshalInterfaceHelper<global::System.EventHandler>.CreateMarshalerArray2(array, CreateMarshaler2);
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.EventHandler>.GetAbiArray(box);
        public static unsafe global::System.EventHandler[] FromAbiArray(object box) => MarshalInterfaceHelper<global::System.EventHandler>.FromAbiArray(box, FromAbi);
        public static void CopyAbiArray(global::System.EventHandler[] array, object box) => MarshalInterfaceHelper<global::System.EventHandler>.CopyAbiArray(array, box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::System.EventHandler[] array) => MarshalInterfaceHelper<global::System.EventHandler>.FromManagedArray(array, FromManaged);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::System.EventHandler>.MarshalerArray array) => MarshalInterfaceHelper<global::System.EventHandler>.DisposeMarshalerArray(array);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);

#if NET
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#endif
        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr args)
        {
            try
            {
#if NET
                var invoke = ComWrappersSupport.FindObject<global::System.EventHandler>(thisPtr);
                invoke.Invoke(
                    MarshalInspectable<object>.FromAbi(sender),
                    MarshalInspectable<object>.FromAbi(args) as EventArgs ?? EventArgs.Empty);
#else
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::System.EventHandler invoke) =>
                {
                    invoke.Invoke(
                        MarshalInspectable<object>.FromAbi(sender),
                        MarshalInspectable<object>.FromAbi(args) as EventArgs ?? EventArgs.Empty);
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

#if NET
        [SkipLocalsInit]
        internal static ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            Span<ComWrappers.ComInterfaceEntry> entries = stackalloc ComWrappers.ComInterfaceEntry[3];
            int count = 0;

            entries[count++] = new ComWrappers.ComInterfaceEntry
            {
                IID = IID,
                Vtable = AbiToProjectionVftablePtr
            };

            if (FeatureSwitches.EnableIReferenceSupport)
            {
                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = global::WinRT.Interop.IID.IID_IPropertyValue,
                    Vtable = ABI.Windows.Foundation.ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
                };

                entries[count++] = new ComWrappers.ComInterfaceEntry
                {
                    IID = Nullable_EventHandler.IID,
                    Vtable = Nullable_EventHandler.AbiToProjectionVftablePtr
                };
            }

            return entries.Slice(0, count).ToArray();
        }
#endif
    }
}
