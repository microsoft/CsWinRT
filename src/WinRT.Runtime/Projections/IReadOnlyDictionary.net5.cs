// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{

    [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
    interface IMapView<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        V Lookup(K key);
        bool HasKey(K key);
        void Split(out IMapView<K, V> first, out IMapView<K, V> second);
        uint Size { get; }
    }
}

namespace System.Collections.Generic
{
    internal sealed class IReadOnlyDictionaryImpl<K, V> : IReadOnlyDictionary<K, V>, IWinRTObject
    {
        private readonly IObjectReference _inner;

        internal IReadOnlyDictionaryImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        public static IReadOnlyDictionaryImpl<K, V> CreateRcw(IInspectable obj) => new(obj.ObjRef);

        private volatile IObjectReference __iReadOnlyDictionaryObjRef;
        private IObjectReference Make_IDictionaryObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iReadOnlyDictionaryObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.PIID), null);
            return __iReadOnlyDictionaryObjRef;
        }
        private IObjectReference iReadOnlyDictionaryObjRef => __iReadOnlyDictionaryObjRef ?? Make_IDictionaryObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumerableMethods<KeyValuePair<K, V>>.PIID), null);
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

        public V this[K key] => ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.Indexer_Get(iReadOnlyDictionaryObjRef, key);

        public IEnumerable<K> Keys => ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.get_Keys(iReadOnlyDictionaryObjRef);

        public IEnumerable<V> Values => ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.get_Values(iReadOnlyDictionaryObjRef);

        public int Count => ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.get_Count(iReadOnlyDictionaryObjRef);

        public bool ContainsKey(K key) => ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.ContainsKey(iReadOnlyDictionaryObjRef, key);

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => ABI.System.Collections.Generic.IEnumerableMethods<KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);

        public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
        {
            return ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<K, V>.TryGetValue(iReadOnlyDictionaryObjRef, key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace ABI.Windows.Foundation.Collections
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    internal static class IMapViewMethods<K, V>
    {
        // These function pointers will be set by IReadOnlyDictionaryMethods<K, V, KAbi, VAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal volatile unsafe static delegate*<IObjectReference, K, V> _Lookup;
        internal volatile unsafe static delegate*<IObjectReference, K, bool> _HasKey;
        internal volatile unsafe static delegate*<
            IObjectReference, 
            out global::Windows.Foundation.Collections.IMapView<K, V>,
            out global::Windows.Foundation.Collections.IMapView<K, V>,
            void> _Split;
        internal volatile static bool _RcwHelperInitialized;

        public static unsafe V Lookup(IObjectReference obj, K key)
        {
            return _Lookup(obj, key);
        }

        public static unsafe bool HasKey(IObjectReference obj, K key)
        {
            return _HasKey(obj, key);
        }

        public static unsafe void Split(IObjectReference obj, out global::Windows.Foundation.Collections.IMapView<K, V> first, out global::Windows.Foundation.Collections.IMapView<K, V> second)
        {
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                _Split(obj, out first, out second);

                return;
            }

            if (_Split != null)
            {
                _Split(obj, out first, out second);
            }
            else
            {
                var ThisPtr = obj.ThisPtr;

                IntPtr __first = default;
                IntPtr __second = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, IntPtr*, int>**)ThisPtr)[9](ThisPtr, &__first, &__second));
                    first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__first);
                    second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__second);
                }
                finally
                {
                    MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__first);
                    MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__second);
                }
            }
        }

        public static unsafe uint get_Size(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;

            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
            return __retval;   
        }
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
    static class IReadOnlyDictionaryMethods<K, V>
    {
        unsafe static IReadOnlyDictionaryMethods()
        {
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>), typeof(global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>));
            ComWrappersSupport.RegisterHelperType(typeof(global::Windows.Foundation.Collections.IMapView<K, V>), typeof(global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>));

            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return;
            }

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            InitRcwHelperFallbackIfNeeded();
#pragma warning restore IL3050

#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void InitRcwHelperFallbackIfNeeded()
            {
                // Handle the compat scenario where the source generator wasn't used and IDIC hasn't been used yet
                // and due to that the function pointers haven't been initialized.
                if (!ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>._RcwHelperInitialized)
                {
                    var initRcwHelperFallback = (Func<bool>)typeof(IReadOnlyDictionaryMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                        GetMethod("InitRcwHelperFallback", BindingFlags.NonPublic | BindingFlags.Static).
                        CreateDelegate(typeof(Func<bool>));
                    initRcwHelperFallback();
                }
            }
        }

        public static int get_Count(IObjectReference obj)
        {
            uint size = ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>.get_Size(obj);

            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
            }

            return (int)size;
        }

        public static V Indexer_Get(IObjectReference obj, K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return Lookup(obj, key);
        }

        public static global::System.Collections.Generic.IEnumerable<K> get_Keys(IObjectReference obj)
        {
            return new ReadOnlyDictionaryKeyCollection(obj);
        }

        public static global::System.Collections.Generic.IEnumerable<V> get_Values(IObjectReference obj)
        {
            return new ReadOnlyDictionaryValueCollection(obj);
        }

        public static bool ContainsKey(IObjectReference obj, K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>.HasKey(obj, key);
        }

        public static bool TryGetValue(IObjectReference obj, K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            // It may be faster to call HasKey then Lookup.  On failure, we would otherwise
            // throw an exception from Lookup.
            if (!ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>.HasKey(obj, key))
            {
                value = default!;
                return false;
            }

            try
            {
                value = ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>.Lookup(obj, key);
                return true;
            }
            catch (Exception ex)  // Still may hit this case due to a race condition
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                {
                    value = default!;
                    return false;
                }
                throw;
            }
        }

        public static V Lookup(IObjectReference obj, K key)
        {
            try
            {
                return ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>.Lookup(obj, key);
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(String.Format(WinRTRuntimeErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                throw;
            }
        }

        private sealed class ReadOnlyDictionaryKeyCollection : global::System.Collections.Generic.IEnumerable<K>
        {
            private readonly IObjectReference iReadOnlyDictionaryObjRef;

            public ReadOnlyDictionaryKeyCollection(IObjectReference iReadOnlyDictionaryObjRef)
            {
                if (iReadOnlyDictionaryObjRef == null)
                    throw new ArgumentNullException(nameof(iReadOnlyDictionaryObjRef));

                this.iReadOnlyDictionaryObjRef = iReadOnlyDictionaryObjRef;
            }

            private volatile IObjectReference __iEnumerableObjRef;
            private IObjectReference Make_IEnumerableObjRef()
            {
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iReadOnlyDictionaryObjRef.As<IUnknownVftbl>(
                    ABI.System.Collections.Generic.IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.PIID), null);
                return __iEnumerableObjRef;
            }
            private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

            public global::System.Collections.Generic.IEnumerator<K> GetEnumerator() => new ReadOnlyDictionaryKeyEnumerator(iEnumerableObjRef);                                                            

            IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ReadOnlyDictionaryKeyEnumerator : global::System.Collections.Generic.IEnumerator<K>
            {
                private readonly IObjectReference iEnumerableObjRef;
                private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

                public ReadOnlyDictionaryKeyEnumerator(IObjectReference iEnumerableObjRef)
                {
                    this.iEnumerableObjRef = iEnumerableObjRef;
                    enumeration = IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
                }

                void IDisposable.Dispose()
                {
                    enumeration.Dispose();
                }

                public bool MoveNext()
                {
                    return enumeration.MoveNext();
                }

                object IEnumerator.Current => Current;

                public K Current => enumeration.Current.Key;

                public void Reset()
                {
                    enumeration = IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
                }
            }
        }

        private sealed class ReadOnlyDictionaryValueCollection : global::System.Collections.Generic.IEnumerable<V>
        {
            private readonly IObjectReference iDictionaryObjRef;

            public ReadOnlyDictionaryValueCollection(IObjectReference iDictionaryObjRef)
            {
                this.iDictionaryObjRef = iDictionaryObjRef;
            }

            private volatile IObjectReference __iEnumerableObjRef;
            private IObjectReference Make_IEnumerableObjRef()
            {
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iDictionaryObjRef.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.PIID), null);
                return __iEnumerableObjRef;
            }
            private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

            public global::System.Collections.Generic.IEnumerator<V> GetEnumerator()
            {
                return new ReadOnlyDictionaryValueEnumerator(iEnumerableObjRef);
            }
            global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ReadOnlyDictionaryValueEnumerator : global::System.Collections.Generic.IEnumerator<V>
            {
                private readonly IObjectReference iEnumerableObjRef;
                private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

                public ReadOnlyDictionaryValueEnumerator(IObjectReference iEnumerableObjRef)
                {
                    this.iEnumerableObjRef = iEnumerableObjRef;
                    enumeration = IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
                }

                void IDisposable.Dispose()
                {
                    enumeration.Dispose();
                }

                public bool MoveNext()
                {
                    return enumeration.MoveNext();
                }

                object IEnumerator.Current => Current;

                public V Current => enumeration.Current.Value;

                public void Reset()
                {
                    enumeration = IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
                }
            }
        }

        private static IntPtr abiToProjectionVftablePtr;
        public static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
        }

        internal readonly static Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IReadOnlyDictionary<K, V>));
        public static Guid IID => PIID;

        public static V Abi_Lookup_0(IntPtr thisPtr, K key)
        {
            return IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).Lookup(key);
        }

        public static bool Abi_HasKey_2(IntPtr thisPtr, K key)
        {
            return IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).HasKey(key);
        }

        // Modified signature due to Windows.Foundation.Collections.IMapView is not public
        public static void Abi_Split_3(IntPtr thisPtr, out IntPtr first, out IntPtr second)
        {
            global::Windows.Foundation.Collections.IMapView<K, V> __first;
            global::Windows.Foundation.Collections.IMapView<K, V> __second;

            IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).Split(out __first, out __second);

            first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__first);
            second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__second);
        }

        public static uint Abi_get_Size_1(IntPtr thisPtr)
        {
            return IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).Size;
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class IReadOnlyDictionaryMethods<K, KAbi, V, VAbi> where KAbi : unmanaged where VAbi : unmanaged
    {
        public unsafe static bool InitRcwHelper(
            delegate*<IObjectReference, K, V> lookup,
            delegate*<IObjectReference, K, bool> hasKey,
            delegate*<IObjectReference, 
                      out global::System.Collections.Generic.IReadOnlyDictionary<K, V>, 
                      out global::System.Collections.Generic.IReadOnlyDictionary<K, V>, 
                      void> _)
        {
            if (ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>._RcwHelperInitialized)
            {
                return true;
            }

            ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>._Lookup = lookup;
            ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>._HasKey = hasKey;

            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>),
                IReadOnlyDictionaryImpl<K, V>.CreateRcw);
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>), typeof(global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>));
            ComWrappersSupport.RegisterHelperType(typeof(global::Windows.Foundation.Collections.IMapView<K, V>), typeof(global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>));

            ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>._RcwHelperInitialized = true;
            return true;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private unsafe static bool InitRcwHelperFallback()
        {
            return InitRcwHelper(&LookupDynamic, &HasKeyDynamic, null);
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe V LookupDynamic(IObjectReference obj, K key)
        {
            var ThisPtr = obj.ThisPtr;
            object __key = default;
            VAbi valueAbi = default;
            var __params = new object[] { ThisPtr, null, (IntPtr)(void*)&valueAbi };
            try
            {
                __key = Marshaler<K>.CreateMarshaler2(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                DelegateHelper.Get(obj).Lookup.DynamicInvokeAbi(__params);
                return Marshaler<V>.FromAbi(valueAbi);
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeAbi(valueAbi);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe bool HasKeyDynamic(IObjectReference obj, K key)
        {
            var ThisPtr = obj.ThisPtr;
            object __key = default;
            byte found;
            var __params = new object[] { ThisPtr, null, (IntPtr)(void*)&found };
            try
            {
                __key = Marshaler<K>.CreateMarshaler2(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                DelegateHelper.Get(obj).HasKey.DynamicInvokeAbi(__params);
                return found != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
            }
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi*, int> lookup,
            delegate* unmanaged[Stdcall]<IntPtr, uint*, int> getSize,
            delegate* unmanaged[Stdcall]<IntPtr, KAbi, byte*, int> hasKey,
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, IntPtr*, int> split)
        {
            if (IReadOnlyDictionaryMethods<K, V>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 4));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi*, int>*)abiToProjectionVftablePtr)[6] = lookup;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)abiToProjectionVftablePtr)[7] = getSize;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi, byte*, int>*)abiToProjectionVftablePtr)[8] = hasKey;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, IntPtr*, int>*)abiToProjectionVftablePtr)[9] = split;

            if (!IReadOnlyDictionaryMethods<K, V>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
                return false;
            }

            return true;
        }

        private static global::System.Delegate[] DelegateCache;

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        internal static unsafe void InitFallbackCCWVtable()
        {
            DelegateCache = new global::System.Delegate[]
            {
                global::System.Delegate.CreateDelegate(Lookup_0_Type, typeof(IReadOnlyDictionaryMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_Lookup_0), BindingFlags.NonPublic | BindingFlags.Static)),
                new _get_PropertyAsUInt32_Abi(Do_Abi_get_Size_1),
                global::System.Delegate.CreateDelegate(HasKey_2_Type, typeof(IReadOnlyDictionaryMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_HasKey_2), BindingFlags.NonPublic | BindingFlags.Static)),
                new IReadOnlyDictionary_Delegates.Split_3_Abi(Do_Abi_Split_3),
            };

            InitCcw(
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[0]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[1]),
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi, byte*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[2]),
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, IntPtr*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[3])
            );
        }

        private static unsafe int Do_Abi_Lookup_0(IntPtr thisPtr, KAbi key, VAbi* __return_value__)
        {
            V ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).Lookup(Marshaler<K>.FromAbi(key));
                *__return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_HasKey_2(IntPtr thisPtr, KAbi key, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).HasKey(Marshaler<K>.FromAbi(key));
                *__return_value__ = (byte)(____return_value__ ? 1 : 0);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_Split_3(IntPtr thisPtr, IntPtr* first, IntPtr* second)
        {
            *first = default;
            *second = default;
            global::Windows.Foundation.Collections.IMapView<K, V> __first = default;
            global::Windows.Foundation.Collections.IMapView<K, V> __second = default;

            try
            {
                IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).Split(out __first, out __second);
                *first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__first);
                *second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__second);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
        {
            uint ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IReadOnlyDictionary<K, V>.FindAdapter(thisPtr).Size;
                *__return_value__ = ____return_value__;
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static Type _lookup_0_type;
        private static Type Lookup_0_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _lookup_0_type ?? MakeLookupType();
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static Type MakeLookupType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _lookup_0_type, Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(KAbi), typeof(VAbi*), typeof(int) }), null);
            return _lookup_0_type;
        }

        private static Type _hasKey_2_type;
        private static Type HasKey_2_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _hasKey_2_type ?? MakeHasKeyType();
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static Type MakeHasKeyType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _hasKey_2_type, Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(KAbi), typeof(byte*), typeof(int) }), null);
            return _hasKey_2_type;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private sealed class DelegateHelper
        {
            private readonly IntPtr _ptr;

            private Delegate _lookupDelegate;
            public Delegate Lookup => _lookupDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _lookupDelegate, Lookup_0_Type, 6);

            private Delegate _hasKeyDelegate;
            public Delegate HasKey => _hasKeyDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _hasKeyDelegate, HasKey_2_Type, 8);

            private DelegateHelper(IntPtr ptr)
            {
                _ptr = ptr;
            }

            public static DelegateHelper Get(IObjectReference obj)
            {
                return (DelegateHelper)GenericDelegateHelper.DelegateTable.GetValue(obj, static (objRef) => new DelegateHelper(objRef.ThisPtr));
            }
        }
    }

    internal sealed class ConstantSplittableMap<K, V> : global::Windows.Foundation.Collections.IMapView<K, V>, global::System.Collections.Generic.IReadOnlyDictionary<K, V>
    {
        private sealed class KeyValuePairComparator : IComparer<global::System.Collections.Generic.KeyValuePair<K, V>>
        {
            private static readonly IComparer<K> keyComparator = Comparer<K>.Default;

            public int Compare(global::System.Collections.Generic.KeyValuePair<K, V> x, global::System.Collections.Generic.KeyValuePair<K, V> y)
            {
                return keyComparator.Compare(x.Key, y.Key);
            }
        }

        private static readonly KeyValuePairComparator keyValuePairComparator = new KeyValuePairComparator();

        private readonly global::System.Collections.Generic.KeyValuePair<K, V>[] items;
        private readonly int firstItemIndex;
        private readonly int lastItemIndex;

        internal ConstantSplittableMap(global::System.Collections.Generic.IReadOnlyDictionary<K, V> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            firstItemIndex = 0;
            lastItemIndex = data.Count - 1;
            items = CreateKeyValueArray(data.Count, data.GetEnumerator());
        }

        private ConstantSplittableMap(global::System.Collections.Generic.KeyValuePair<K, V>[] items, int firstItemIndex, int lastItemIndex)
        {
            this.items = items;
            this.firstItemIndex = firstItemIndex;
            this.lastItemIndex = lastItemIndex;
        }

        private global::System.Collections.Generic.KeyValuePair<K, V>[] CreateKeyValueArray(int count, global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> data)
        {
            global::System.Collections.Generic.KeyValuePair<K, V>[] kvArray = new global::System.Collections.Generic.KeyValuePair<K, V>[count];

            int i = 0;
            while (data.MoveNext())
                kvArray[i++] = data.Current;

            Array.Sort(kvArray, keyValuePairComparator);

            return kvArray;
        }

        public uint Size => (uint)(lastItemIndex - firstItemIndex + 1);

        public global::System.Collections.Generic.IEnumerable<K> Keys
        {
            get => new ReadOnlyDictionaryKeyCollection<K, V>(this);
        }

        public global::System.Collections.Generic.IEnumerable<V> Values
        {
            get => new ReadOnlyDictionaryValueCollection<K, V>(this);
        }

        public int Count => lastItemIndex - firstItemIndex + 1;

        public V this[K key] => Lookup(key);

        public V Lookup(K key)
        {
            V value;
            bool found = TryGetValue(key, out value);

            if (!found)
            {
                Exception e = new KeyNotFoundException(String.Format(WinRTRuntimeErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                e.SetHResult(ExceptionHelpers.E_BOUNDS);
                throw e;
            }

            return value;
        }

        public bool HasKey(K key) =>
            TryGetValue(key, out _);

        public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> First() => GetEnumerator();

        public void Split(out global::Windows.Foundation.Collections.IMapView<K, V> firstPartition, out global::Windows.Foundation.Collections.IMapView<K, V> secondPartition)
        {
            if (Count < 2)
            {
                firstPartition = null;
                secondPartition = null;
                return;
            }

            int pivot = (int)(((long)firstItemIndex + (long)lastItemIndex) / (long)2);

            firstPartition = new ConstantSplittableMap<K, V>(items, firstItemIndex, pivot);
            secondPartition = new ConstantSplittableMap<K, V>(items, pivot + 1, lastItemIndex);
        }

        public bool TryGetValue(K key, out V value)
        {
            var searchKey = new global::System.Collections.Generic.KeyValuePair<K, V>(key, default!);
            int index = Array.BinarySearch(items, firstItemIndex, Count, searchKey, keyValuePairComparator);

            if (index < 0)
            {
                value = default!;
                return false;
            }

            value = items[index].Value;
            return true;
        }

        public bool ContainsKey(K key)
        {
            return HasKey(key);
        }

        global::System.Collections.Generic.IEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> global::Windows.Foundation.Collections.IIterable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.First()
        {
            var itemsAsIKeyValuePairs = new global::Windows.Foundation.Collections.IKeyValuePair<K, V>[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                itemsAsIKeyValuePairs[i] = new KeyValuePair<K, V>.ToIKeyValuePair(ref items[i]);
            }
            return new ConstantSplittableMapEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>(itemsAsIKeyValuePairs, firstItemIndex, lastItemIndex);
        }

        public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> GetEnumerator()
        {
            return new ConstantSplittableMapEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>>(items, firstItemIndex, lastItemIndex);
        }

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
        {
            return new ConstantSplittableMapEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>>(items, firstItemIndex, lastItemIndex);
        }
    }

    internal struct ConstantSplittableMapEnumerator<T> : global::System.Collections.Generic.IEnumerator<T>
    {
        private readonly T[] _array;
        private readonly int _start;
        private readonly int _end;
        private int _current;

        internal ConstantSplittableMapEnumerator(T[] items, int first, int end)
        {
            _array = items;
            _start = first;
            _end = end;
            _current = _start - 1;
        }

        public bool MoveNext()
        {
            if (_current < _end)
            {
                _current++;
                return true;
            }
            return false;
        }

        public T Current
        {
            get
            {
                if (_current < _start) throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumNotStarted);
                if (_current > _end) throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_EnumEnded);
                return _array[_current];
            }
        }

        object IEnumerator.Current => Current;

        void IEnumerator.Reset() =>
            _current = _start - 1;

        public void Dispose()
        {
        }
    }

    internal sealed class ReadOnlyDictionaryKeyCollection<K, V> : global::System.Collections.Generic.IEnumerable<K>
    {
        private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary;

        public ReadOnlyDictionaryKeyCollection(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public global::System.Collections.Generic.IEnumerator<K> GetEnumerator()
        {
            return new ReadOnlyDictionaryKeyEnumerator(dictionary);
        }
        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class ReadOnlyDictionaryKeyEnumerator : global::System.Collections.Generic.IEnumerator<K>
        {
            private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary;
            private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

            public ReadOnlyDictionaryKeyEnumerator(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
                enumeration = dictionary.GetEnumerator();
            }

            void IDisposable.Dispose()
            {
                enumeration.Dispose();
            }

            public bool MoveNext()
            {
                return enumeration.MoveNext();
            }

            object IEnumerator.Current => Current;

            public K Current => enumeration.Current.Key;

            public void Reset()
            {
                enumeration = dictionary.GetEnumerator();
            }
        }
    }

    internal sealed class ReadOnlyDictionaryValueCollection<K, V> : global::System.Collections.Generic.IEnumerable<V>
    {
        private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary;

        public ReadOnlyDictionaryValueCollection(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public global::System.Collections.Generic.IEnumerator<V> GetEnumerator()
        {
            return new ReadOnlyDictionaryValueEnumerator(dictionary);
        }
        global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class ReadOnlyDictionaryValueEnumerator : global::System.Collections.Generic.IEnumerator<V>
        {
            private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary;
            private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

            public ReadOnlyDictionaryValueEnumerator(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary)
            {
                this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
                enumeration = dictionary.GetEnumerator();
            }

            void IDisposable.Dispose()
            {
                enumeration.Dispose();
            }

            public bool MoveNext()
            {
                return enumeration.MoveNext();
            }

            object IEnumerator.Current => Current;

            public V Current => enumeration.Current.Value;

            public void Reset()
            {
                enumeration = dictionary.GetEnumerator();
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
#pragma warning disable CA2256 // Not implementing IMapView<K, V> for [DynamicInterfaceCastableImplementation], as we don't expect to need IDIC for WinRT types
    interface IReadOnlyDictionary<K, V> : global::System.Collections.Generic.IReadOnlyDictionary<K, V>, global::Windows.Foundation.Collections.IMapView<K, V>
#pragma warning restore CA2256
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyDictionary<K, V> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IReadOnlyDictionary<K, V> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IReadOnlyDictionary<K, V> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReadOnlyDictionary<K, V>));

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IMapView<K, V>
#pragma warning restore CA2257
        {
            private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> _dictionary;

            internal ToAbiHelper(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary) => _dictionary = dictionary;

            uint global::Windows.Foundation.Collections.IMapView<K, V>.Size { get => (uint)_dictionary.Count; }

            global::System.Collections.Generic.IEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> global::Windows.Foundation.Collections.IIterable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.First() =>
                new KeyValuePair<K, V>.Enumerator(_dictionary.GetEnumerator());

            public V Lookup(K key)
            {
                V value;
                bool keyFound = _dictionary.TryGetValue(key, out value);

                if (!keyFound)
                {
                    Exception e = new KeyNotFoundException(String.Format(WinRTRuntimeErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }

                return value;
            }

            public uint Size() => (uint)_dictionary.Count;

            public bool HasKey(K key) => _dictionary.ContainsKey(key);

            void global::Windows.Foundation.Collections.IMapView<K, V>.Split(out global::Windows.Foundation.Collections.IMapView<K, V> first, out global::Windows.Foundation.Collections.IMapView<K, V> second)
            {
                if (_dictionary.Count < 2)
                {
                    first = null;
                    second = null;
                    return;
                }

                if (!(_dictionary is ConstantSplittableMap<K, V> splittableMap))
                    splittableMap = new ConstantSplittableMap<K, V>(_dictionary);

                splittableMap.Split(out first, out second);
            }
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IReadOnlyDictionary()
        {
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                // Simple invocation guarded by a direct runtime feature check to help the linker.
                // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                InitFallbackCCWVTableIfNeeded();
#pragma warning restore IL3050

#if NET8_0_OR_GREATER
                [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
#if NET
                [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void InitFallbackCCWVTableIfNeeded()
                {
                    if (IReadOnlyDictionaryMethods<K, V>.AbiToProjectionVftablePtr == default)
                    {
                        // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                        var initFallbackCCWVtable = (Action)typeof(IReadOnlyDictionaryMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                            GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                            CreateDelegate(typeof(Action));
                        initFallbackCCWVtable();
                    }
                }
            }

            AbiToProjectionVftablePtr = IReadOnlyDictionaryMethods<K, V>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public unsafe struct Vftbl
#pragma warning restore CA2257
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.AbiToProjectionVftablePtr;

            public static Guid PIID = ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.PIID;
        }

        private static readonly ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyDictionary<K, V>, ToAbiHelper> _adapterTable = new();

        internal static global::Windows.Foundation.Collections.IMapView<K, V> FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IReadOnlyDictionary<K, V>>(thisPtr);
            return _adapterTable.GetValue(__this, (dictionary) => new ToAbiHelper(dictionary));
        }

        public static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, IID.IID_IUnknown);
        }

        public static Guid PIID = IReadOnlyDictionaryMethods<K, V>.PIID;

        global::System.Collections.Generic.IEnumerable<K> global::System.Collections.Generic.IReadOnlyDictionary<K, V>.Keys
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle);
                return IReadOnlyDictionaryMethods<K, V>.get_Keys(_obj);
            }
        }
        global::System.Collections.Generic.IEnumerable<V> global::System.Collections.Generic.IReadOnlyDictionary<K, V>.Values
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle);
                return IReadOnlyDictionaryMethods<K, V>.get_Values(_obj);
            }
        }
        int global::System.Collections.Generic.IReadOnlyCollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Count
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle);
                return IReadOnlyDictionaryMethods<K, V>.get_Count(_obj);
            }
        }
        V global::System.Collections.Generic.IReadOnlyDictionary<K, V>.this[K key]
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle);
                return IReadOnlyDictionaryMethods<K, V>.Indexer_Get(_obj, key);
            }
        }
        bool global::System.Collections.Generic.IReadOnlyDictionary<K, V>.ContainsKey(K key)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle);
            return IReadOnlyDictionaryMethods<K, V>.ContainsKey(_obj, key);
        }
        bool global::System.Collections.Generic.IReadOnlyDictionary<K, V>.TryGetValue(K key, out V value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle);
            return IReadOnlyDictionaryMethods<K, V>.TryGetValue(_obj, key, out value);
        }
        global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()
        {
            ((IWinRTObject)this).IsInterfaceImplemented(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle, true);
            var _objEnumerable = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle);
            return IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(_objEnumerable);
        }

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

#if EMBED
    internal
#else
    public
#endif
    static class IReadOnlyDictionary_Delegates
    {
        public unsafe delegate int Split_3(IntPtr thisPtr, out IntPtr first, out IntPtr second);

        internal unsafe delegate int Split_3_Abi(IntPtr thisPtr, IntPtr* first, IntPtr* second);
    }
}
