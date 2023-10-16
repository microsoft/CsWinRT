// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Interop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Foundation.Collections;
using WinRT;
using WinRT.Interop;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{

    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    internal interface IIterable<T>
    {
        IEnumerator<T> First(); // Combining IIterable & IEnumerator needs redesign
    }


    [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
    internal interface IIterator<T>
    {
        bool _MoveNext();
        uint GetMany(ref T[] items);
        T _Current { get; }
        bool HasCurrent { get; }
    }
}

namespace ABI.Windows.Foundation.Collections
{
    internal static class IIterableMethods<T>
    {
        // These function pointers will be set by IEnumerableMethods<T,TAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal unsafe static delegate*<bool> _EnsureEnumeratorInitialized;

        internal static unsafe bool EnsureInitialized()
        {
            // Handle the compat scenario where the source generator wasn't used and IDIC hasn't been used yet
            // and due to that the function pointers haven't been initialized.
            if (_EnsureEnumeratorInitialized == null)
            {
                var ensureInitializedFallback = (Func<bool>)typeof(ABI.System.Collections.Generic.IEnumerableMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                    GetMethod("EnsureRcwHelperInitialized", BindingFlags.Public | BindingFlags.Static).
                    CreateDelegate(typeof(Func<bool>));
                ensureInitializedFallback();
            }
            return true;
        }

        public unsafe static IEnumerator<T> First(IObjectReference obj)
        {
            IntPtr __retval = default;
            try
            {
                var ThisPtr = obj.ThisPtr;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval));
                EnsureInitialized();
                _ = _EnsureEnumeratorInitialized();
                return ABI.System.Collections.Generic.FromAbiEnumerator<T>.FromAbi(__retval);
            }
            finally
            {
                ABI.System.Collections.Generic.FromAbiEnumerator<T>.DisposeAbi(__retval);
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    internal interface IIterable<T> : ABI.System.Collections.Generic.IEnumerable<T>
    {
        public static new Guid PIID = ABI.System.Collections.Generic.IEnumerableMethods<T>.PIID;
    }
}

namespace System.Collections.Generic
{
    internal sealed class IEnumerableImpl<T> : IEnumerable<T>, IWinRTObject
    {
        private readonly IObjectReference _inner;

        internal IEnumerableImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        public static IEnumerableImpl<T> CreateRcw(IInspectable obj) => new(obj.ObjRef);

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumerableMethods<T>.PIID), null);
            return __iEnumerableObjRef;
        }
        private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

        IObjectReference IWinRTObject.NativeObject => _inner;

        bool IWinRTObject.HasUnwrappableNativeObject => true;

        private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
        private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
            return _queryInterfaceCache;
        }
        global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
        private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
        private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
            return _additionalTypeData;
        }
        global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();

        public IEnumerator<T> GetEnumerator()
        {
            return global::ABI.System.Collections.Generic.IEnumerableMethods<T>.GetEnumerator(iEnumerableObjRef);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class IEnumeratorImpl<T> : IEnumerator<T>, IIterator<T>, IWinRTObject
    {
        private readonly IObjectReference _inner;

        internal IEnumeratorImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        public static IEnumeratorImpl<T> CreateRcw(IInspectable obj) => new(obj.ObjRef);

        private volatile IObjectReference __iEnumeratorObjRef;
        private IObjectReference Make_IEnumeratorObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumeratorObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumeratorMethods<T>.PIID), null);
            return __iEnumeratorObjRef;
        }
        private IObjectReference iEnumeratorObjRef => __iEnumeratorObjRef ?? Make_IEnumeratorObjRef();

        IObjectReference IWinRTObject.NativeObject => _inner;

        bool IWinRTObject.HasUnwrappableNativeObject => true;

        private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
        private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
            return _queryInterfaceCache;
        }
        global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
        private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
        private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
            return _additionalTypeData;
        }

        global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();

        bool IEnumerator.MoveNext()
        {
            return global::ABI.System.Collections.Generic.IEnumeratorMethods<T>.MoveNext(iEnumeratorObjRef);
        }

        void IEnumerator.Reset()
        {
            global::ABI.System.Collections.Generic.IEnumeratorMethods<T>.Reset(iEnumeratorObjRef);
        }

        void IDisposable.Dispose()
        {
            global::ABI.System.Collections.Generic.IEnumeratorMethods<T>.Dispose(iEnumeratorObjRef);
        }

        bool IIterator<T>._MoveNext()
        {
            return global::ABI.System.Collections.Generic.IIteratorMethods<T>.MoveNext(iEnumeratorObjRef);
        }

        uint IIterator<T>.GetMany(ref T[] items)
        {
            return global::ABI.System.Collections.Generic.IIteratorMethods<T>.GetMany(iEnumeratorObjRef, ref items);
        }

        public T Current => global::ABI.System.Collections.Generic.IEnumeratorMethods<T>.get_Current(iEnumeratorObjRef);

        object IEnumerator.Current => Current;

        T IIterator<T>._Current => global::ABI.System.Collections.Generic.IIteratorMethods<T>.get_Current(iEnumeratorObjRef);

        bool IIterator<T>.HasCurrent => global::ABI.System.Collections.Generic.IIteratorMethods<T>.get_HasCurrent(iEnumeratorObjRef);
    }
}

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

#if EMBED
    internal
#else
    public
#endif
    static class IEnumerableMethods<T>
    {
        public static global::System.Collections.Generic.IEnumerator<T> GetEnumerator(IObjectReference obj)
        {
            var first = ABI.Windows.Foundation.Collections.IIterableMethods<T>.First(obj);
            if (first is global::ABI.System.Collections.Generic.FromAbiEnumerator<T> iterator)
            {
                return iterator;
            }
            throw new InvalidOperationException("Unexpected type for enumerator");
        }

        private static IntPtr abiToProjectionVftablePtr;
        public static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
        }

        public static global::System.Collections.Generic.IEnumerator<T> Abi_First_0(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IEnumerable<T>>(thisPtr);
            return __this.GetEnumerator();
        }

        internal readonly static Guid PIID = GuidGenerator.CreateIID(typeof(IEnumerable<T>));
        public static Guid IID => PIID;
    }

#if EMBED
    internal
#else
    public
#endif
    static class IEnumerableMethods<T, TAbi> where TAbi : unmanaged
    {
        internal static bool RcwHelperInitialized { get; } = InitRcwHelper();

        internal unsafe static bool InitRcwHelper()
        {         
            ABI.Windows.Foundation.Collections.IIterableMethods<T>._EnsureEnumeratorInitialized = &IEnumeratorMethods<T, TAbi>.EnsureRcwHelperInitialized;
            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.IEnumerable<T>),
                IEnumerableImpl<T>.CreateRcw);
            return true;
        }

        public static bool EnsureRcwHelperInitialized()
        {
            return RcwHelperInitialized;
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> first)
        {
            if (IEnumerableMethods<T>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)abiToProjectionVftablePtr)[6] = first;

            if (!IEnumerableMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
                return false;
            }

            return true;
        }

        private static IEnumerable_Delegates.First_0_Abi DelegateCache;

        internal static unsafe void InitFallbackCCWVtable()
        {
            DelegateCache = new IEnumerable_Delegates.First_0_Abi(Do_Abi_First_0);

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((IntPtr*)abiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(DelegateCache);

            if (!IEnumerableMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
            }
        }

        private static unsafe int Do_Abi_First_0(IntPtr thisPtr, IntPtr* __return_value__)
        {
            *__return_value__ = default;
            try
            {
                *__return_value__ = MarshalInterface<global::System.Collections.Generic.IEnumerator<T>>.FromManaged(IEnumerableMethods<T>.Abi_First_0(thisPtr));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
    interface IEnumerable<T> : global::System.Collections.Generic.IEnumerable<T>, global::Windows.Foundation.Collections.IIterable<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IEnumerable<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IEnumerable<T> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IEnumerable<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IIterable<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerable<T>));

        internal sealed class ToAbiHelper : global::Windows.Foundation.Collections.IIterable<T>
        {
            private readonly IEnumerable<T> m_enumerable;

            internal ToAbiHelper(IEnumerable<T> enumerable) => m_enumerable = enumerable;

            public global::System.Collections.Generic.IEnumerator<T> First() => m_enumerable.GetEnumerator();
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IEnumerable()
        {
            if (IEnumerableMethods<T>.AbiToProjectionVftablePtr == default)
            {
                // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                var initFallbackCCWVtable = (Action)typeof(IEnumerableMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                    GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                    CreateDelegate(typeof(Action));
                initFallbackCCWVtable();
            }

            AbiToProjectionVftablePtr = IEnumerableMethods<T>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("FAA585EA-6214-4217-AFDA-7F46DE5869B3")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.IEnumerable<T>.AbiToProjectionVftablePtr;

            public static Guid PIID = ABI.System.Collections.Generic.IEnumerable<T>.PIID;
        }

        public static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);
        }

        public static Guid PIID = ABI.System.Collections.Generic.IEnumerableMethods<T>.PIID;

        global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<T>).TypeHandle);
            return IEnumerableMethods<T>.GetEnumerator(_obj);
        }

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

#if EMBED
    internal
#else
    public
#endif
    static class IEnumerable_Delegates
    {
        public unsafe delegate int First_0(IntPtr thisPtr, out IntPtr __return_value__);

        internal unsafe delegate int First_0_Abi(IntPtr thisPtr, IntPtr* __return_value__);
    }

    internal static class IIteratorMethods<T>
    {
        // These function pointers will be set by IEnumeratorMethods<T,TAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal unsafe static delegate*<IntPtr, T> _GetCurrent;

        public static unsafe bool MoveNext(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)ThisPtr)[8](ThisPtr, &__retval));
            return __retval != 0;
        }

        public static unsafe uint GetMany(IObjectReference obj, ref T[] items)
        {
            var ThisPtr = obj.ThisPtr;

            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int>**)ThisPtr)[9](ThisPtr, __items_length, __items_data, &__retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public static unsafe T get_Current(IObjectReference obj)
        {
            return _GetCurrent(obj.ThisPtr);
        }

        public static unsafe bool get_HasCurrent(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)ThisPtr)[7](ThisPtr, &__retval));
            return __retval != 0;
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class IEnumeratorMethods<T>
    {
        public static T get_Current(IObjectReference obj)
        {
            return IIteratorMethods<T>.get_Current(obj);
        }

        public static bool MoveNext(IObjectReference obj)
        {
            return IIteratorMethods<T>.MoveNext(obj);
        }

        public static void Reset(IObjectReference obj)
        {
            throw new NotSupportedException();
        }

        public static void Dispose(IObjectReference obj)
        {
        }

        private static IntPtr abiToProjectionVftablePtr;
        public static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
        }

        public static bool Abi_MoveNext_2(IntPtr thisPtr)
        {
            return IEnumerator<T>.FindAdapter(thisPtr)._MoveNext();
        }

        public static uint Abi_GetMany_3(IntPtr thisPtr, ref T[] items)
        {
            return IEnumerator<T>.FindAdapter(thisPtr).GetMany(ref items);
        }

        public static T Abi_get_Current_0(IntPtr thisPtr)
        {
            return IEnumerator<T>.FindAdapter(thisPtr)._Current;
        }

        public static bool Abi_get_HasCurrent_1(IntPtr thisPtr)
        {
            return IEnumerator<T>.FindAdapter(thisPtr).HasCurrent;
        }

        internal readonly static Guid PIID = GuidGenerator.CreateIID(typeof(IEnumerator<T>));
        public static Guid IID => PIID;
    }

#if EMBED
    internal
#else
    public
#endif
    static class IEnumeratorMethods<T, TAbi> where TAbi : unmanaged
    {
        private static bool RcwHelperInitialized { get; } = InitRcwHelper();

        private unsafe static bool InitRcwHelper()
        {
            IIteratorMethods<T>._GetCurrent = &get_Current;
            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.IEnumerator<T>),
                IEnumeratorImpl<T>.CreateRcw);
            return true;
        }

        public static bool EnsureRcwHelperInitialized()
        {
            return RcwHelperInitialized;
        }

        private unsafe static T get_Current(IntPtr ptr)
        {
            TAbi result = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)ptr)[6](ptr, &result));
                return Marshaler<T>.FromAbi(result);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(result);
            }
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, TAbi*, int> getCurrent,
            delegate* unmanaged[Stdcall]<IntPtr, byte*, int> hasCurrent,
            delegate* unmanaged[Stdcall]<IntPtr, byte*, int> moveNext,
            delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int> getMany)
        {
            if (IEnumeratorMethods<T>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 4));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, TAbi*, int>*)abiToProjectionVftablePtr)[6] = getCurrent;
            ((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>*)abiToProjectionVftablePtr)[7] = hasCurrent;
            ((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>*)abiToProjectionVftablePtr)[8] = moveNext;
            ((delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int>*)abiToProjectionVftablePtr)[9] = getMany;

            if (!IEnumeratorMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
                return false;
            }

            return true;
        }

        private static global::System.Delegate[] DelegateCache;

        internal static unsafe void InitFallbackCCWVtable()
        {
            Type get_Current_0_Type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(TAbi*), typeof(int) });

            DelegateCache = new global::System.Delegate[]
            {
                global::System.Delegate.CreateDelegate(get_Current_0_Type, typeof(IEnumeratorMethods<T,TAbi>).GetMethod(nameof(Do_Abi_get_Current_0), BindingFlags.NonPublic | BindingFlags.Static)),
                new _get_PropertyAsBoolean_Abi(Do_Abi_get_HasCurrent_1),
                new IEnumerator_Delegates.MoveNext_2_Abi(Do_Abi_MoveNext_2),
                new IEnumerator_Delegates.GetMany_3_Abi(Do_Abi_GetMany_3)
            };

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 4));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((IntPtr*)abiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(DelegateCache[0]);
            ((IntPtr*)abiToProjectionVftablePtr)[7] = Marshal.GetFunctionPointerForDelegate(DelegateCache[1]);
            ((IntPtr*)abiToProjectionVftablePtr)[8] = Marshal.GetFunctionPointerForDelegate(DelegateCache[2]);
            ((IntPtr*)abiToProjectionVftablePtr)[9] = Marshal.GetFunctionPointerForDelegate(DelegateCache[3]);

            if (!IEnumeratorMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
            }
        }

        private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IEnumerator<T>.FindAdapter(thisPtr)._MoveNext();
                *__return_value__ = (byte)(____return_value__ ? 1 : 0);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, uint* __return_value__)
        {
            uint ____return_value__ = default;

            *__return_value__ = default;
            T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

            try
            {
                ____return_value__ = IEnumerator<T>.FindAdapter(thisPtr).GetMany(ref __items);
                Marshaler<T>.CopyManagedArray(__items, items);
                *__return_value__ = ____return_value__;

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_Current_0(IntPtr thisPtr, TAbi* __return_value__)
        {
            T ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IEnumerator<T>.FindAdapter(thisPtr)._Current;
                *__return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IEnumerator<T>.FindAdapter(thisPtr).HasCurrent;
                *__return_value__ = (byte)(____return_value__ ? 1 : 0);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    // Used by GetEnumerator to provide an IIterator mapping to C# that follows C# conventions
    // such as the enumerator starting at index -1 rather than index 0.
    internal sealed class FromAbiEnumerator<T> : global::System.Collections.Generic.IEnumerator<T>
    {
        private readonly global::Windows.Foundation.Collections.IIterator<T> _iterator;

        public FromAbiEnumerator(IObjectReference obj) :
            this(new global::System.Collections.Generic.IEnumeratorImpl<T>(obj))
        {
        }

        internal FromAbiEnumerator(global::Windows.Foundation.Collections.IIterator<T> iterator)
        {
            _iterator = iterator;
        }

        public static global::System.Collections.Generic.IEnumerator<T> FromAbi(IntPtr abi)
        {
            if (abi == IntPtr.Zero)
            {
                return null;
            }
            return new FromAbiEnumerator<T>(ObjectReference<IUnknownVftbl>.FromAbi(abi));
        }

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::Windows.Foundation.Collections.IIterator<T>>.DisposeAbi(abi);

        private bool m_hadCurrent = true;
        private T m_current = default!;
        private bool m_isInitialized = false;

        public T Current
        {
            get
            {
                // The enumerator has not been advanced to the first element yet.
                if (!m_isInitialized)
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumNotStarted);
                // The enumerator has reached the end of the collection
                if (!m_hadCurrent)
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumEnded);
                return m_current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                // The enumerator has not been advanced to the first element yet.
                if (!m_isInitialized)
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumNotStarted);
                // The enumerator has reached the end of the collection
                if (!m_hadCurrent)
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumEnded);
                return m_current;
            }
        }

        public bool MoveNext()
        {
            // If we've passed the end of the iteration, IEnumerable<T> should return false, while
            // IIterable will fail the interface call
            if (!m_hadCurrent)
            {
                return false;
            }

            // IIterators start at index 0, rather than -1.  If this is the first call, we need to just
            // check HasCurrent rather than actually moving to the next element
            try
            {
                if (!m_isInitialized)
                {
                    m_hadCurrent = _iterator.HasCurrent;
                    m_isInitialized = true;
                }
                else
                {
                    m_hadCurrent = _iterator._MoveNext();
                }

                // We want to save away the current value for two reasons:
                //  1. Accessing .Current is cheap on other iterators, so having it be a property which is a
                //     simple field access preserves the expected performance characteristics (as opposed to
                //     triggering a COM call every time the property is accessed)
                //
                //  2. This allows us to preserve the same semantics as generic collection iteration when iterating
                //     beyond the end of the collection - namely that Current continues to return the last value
                //     of the collection
                if (m_hadCurrent)
                {
                    m_current = _iterator._Current;
                }
            }
            catch (Exception e)
            {
                // Translate E_CHANGED_STATE into an InvalidOperationException for an updated enumeration
                if (Marshal.GetHRForException(e) == ExceptionHelpers.E_CHANGED_STATE)
                {
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumFailedVersion);
                }
                else
                {
                    throw;
                }
            }

            return m_hadCurrent;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    internal sealed class IBindableIteratorTypeDetails : IWinRTExposedTypeDetails
    {
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            return new ComWrappers.ComInterfaceEntry[]
            {
                    new ComWrappers.ComInterfaceEntry
                    {
                        IID = typeof(ABI.Microsoft.UI.Xaml.Interop.IBindableIterator).GUID,
                        Vtable = ABI.Microsoft.UI.Xaml.Interop.IBindableIterator.AbiToProjectionVftablePtr
                    }
            };
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
    interface IEnumerator<T> : global::System.Collections.Generic.IEnumerator<T>, global::Windows.Foundation.Collections.IIterator<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IEnumerator<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IEnumerator<T> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IEnumerator<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IIterator<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerator<T>));

        // Limiting projected surface to IBindableIterator as we only create a CCW for it during those scenarios.
        // In IEnumerator<> scenarios, we use this as a helper for the implementation and don't actually use it to
        // create a CCW.
        [global::WinRT.WinRTExposedType(typeof(IBindableIteratorTypeDetails))]
        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IIterator<T>, global::Microsoft.UI.Xaml.Interop.IBindableIterator
        {
            private readonly global::System.Collections.Generic.IEnumerator<T> m_enumerator;
            private bool m_firstItem = true;
            private bool m_hasCurrent;

            internal ToAbiHelper(global::System.Collections.Generic.IEnumerator<T> enumerator) => m_enumerator = enumerator;

            public T _Current
            {
                get
                {
                    // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                    // first access to the iterator we need to advance to the first item.
                    if (m_firstItem)
                    {
                        m_firstItem = false;
                        _MoveNext();
                    }

                    if (!m_hasCurrent)
                    {
                        ExceptionHelpers.ThrowExceptionForHR(ExceptionHelpers.E_BOUNDS);
                    }

                    return m_enumerator.Current;
                }
            }

            public bool HasCurrent
            {
                get
                {
                    // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                    // first access to the iterator we need to advance to the first item.
                    if (m_firstItem)
                    {
                        m_firstItem = false;
                        _MoveNext();
                    }

                    return m_hasCurrent;
                }
            }

            public bool _MoveNext()
            {
                try
                {
                    m_hasCurrent = m_enumerator.MoveNext();
                }
                catch (InvalidOperationException)
                {
                    ExceptionHelpers.ThrowExceptionForHR(ExceptionHelpers.E_CHANGED_STATE);
                }

                return m_hasCurrent;
            }

            public uint GetMany(ref T[] items)
            {
                if (items == null)
                {
                    return 0;
                }

                int index = 0;
                while (index < items.Length && HasCurrent)
                {
                    items[index] = _Current;
                    _MoveNext();
                    ++index;
                }

                if (typeof(T) == typeof(string))
                {
                    string[] stringItems = (items as string[])!;

                    // Fill the rest of the array with string.Empty to avoid marshaling failure
                    for (int i = index; i < items.Length; ++i)
                        stringItems[i] = string.Empty;
                }

                return (uint)index;
            }

            public object Current => _Current;

            public bool MoveNext() => _MoveNext();

            uint global::Microsoft.UI.Xaml.Interop.IBindableIterator.GetMany(ref object[] items)
            {
                // Should not be called.
                throw new NotImplementedException();
            }
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IEnumerator()
        {
            if (RuntimeFeature.IsDynamicCodeCompiled && IEnumeratorMethods<T>.AbiToProjectionVftablePtr == default)
            {
                // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                var initFallbackCCWVtable = (Action)typeof(IEnumeratorMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                    GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                    CreateDelegate(typeof(Action));
                initFallbackCCWVtable();
            }

            AbiToProjectionVftablePtr = IEnumeratorMethods<T>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("6A79E863-4300-459A-9966-CBB660963EE1")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.IEnumerator<T>.AbiToProjectionVftablePtr;

            public static Guid PIID = ABI.System.Collections.Generic.IEnumerator<T>.PIID;
        }

        private static readonly ConditionalWeakTable<global::System.Collections.Generic.IEnumerator<T>, ToAbiHelper> _adapterTable = new();

        internal static ToAbiHelper FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IEnumerator<T>>(thisPtr);
            return _adapterTable.GetValue(__this, (enumerator) => new ToAbiHelper(enumerator));
        }

        public static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);
        }
        public static Guid PIID = IEnumeratorMethods<T>.PIID;

        T global::System.Collections.Generic.IEnumerator<T>.Current
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerator<T>).TypeHandle);
                return IEnumeratorMethods<T>.get_Current(_obj);
            }
        }

        bool IEnumerator.MoveNext()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerator<T>).TypeHandle);
            return IEnumeratorMethods<T>.MoveNext(_obj);
        }

        void IEnumerator.Reset()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerator<T>).TypeHandle);
            IEnumeratorMethods<T>.Reset(_obj);
        }

        void IDisposable.Dispose()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerator<T>).TypeHandle);
            IEnumeratorMethods<T>.Dispose(_obj);
        }

        object IEnumerator.Current => Current;
    }

#if EMBED
    internal
#else
    public
#endif
    static class IEnumerator_Delegates
    {
        public unsafe delegate int MoveNext_2(IntPtr thisPtr, out byte __return_value__);
        public unsafe delegate int GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, out uint __return_value__);

        internal unsafe delegate int MoveNext_2_Abi(IntPtr thisPtr, byte* __return_value__);
        internal unsafe delegate int GetMany_3_Abi(IntPtr thisPtr, int __itemsSize, IntPtr items, uint* __return_value__);
    }
}
