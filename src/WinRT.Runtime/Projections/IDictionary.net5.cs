// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{
    //Need to rethink how to name/define this interface
    [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
    interface IMap<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        V Lookup(K key);
        bool HasKey(K key);
        IReadOnlyDictionary<K, V> GetView(); // Combining IMap & IReadOnlyDictionary needs redesign
        bool Insert(K key, V value);
        void _Remove(K key);
        void Clear();
        uint Size { get; }
    }
}

namespace System.Collections.Generic
{

    internal sealed class IDictionaryImpl<K, V> : IDictionary<K, V>, IWinRTObject
    {
        private readonly IObjectReference _inner;

        internal IDictionaryImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        public static IDictionaryImpl<K, V> CreateRcw(IInspectable obj) => new(obj.ObjRef);

        private volatile IObjectReference __iDictionaryObjRef;
        private IObjectReference Make_IDictionaryObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iDictionaryObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IDictionaryMethods<K, V>.PIID), null);
            return __iDictionaryObjRef;
        }
        private IObjectReference iDictionaryObjRef => __iDictionaryObjRef ?? Make_IDictionaryObjRef();

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

        public V this[K key] 
        { 
            get => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Indexer_Get(iDictionaryObjRef, null, key);
            set => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Indexer_Set(iDictionaryObjRef, key, value);
        }

        public ICollection<K> Keys => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.get_Keys(iDictionaryObjRef);

        public ICollection<V> Values => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.get_Values(iDictionaryObjRef);

        public int Count => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef);

        public bool IsReadOnly => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.get_IsReadOnly(iDictionaryObjRef);

        public void Add(K key, V value)
        {
            ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Add(iDictionaryObjRef, key, value);
        }

        public void Add(KeyValuePair<K, V> item)
        {
            ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Add(iDictionaryObjRef, item);
        }

        public void Clear()
        {
            ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Clear(iDictionaryObjRef);
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Contains(iDictionaryObjRef, null, item);
        }

        public bool ContainsKey(K key)
        {
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.ContainsKey(iDictionaryObjRef, key);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            ABI.System.Collections.Generic.IDictionaryMethods<K, V>.CopyTo(iDictionaryObjRef, iEnumerableObjRef, array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return ABI.System.Collections.Generic.IEnumerableMethods<KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
        }

        public bool Remove(K key)
        {
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Remove(iDictionaryObjRef, key);
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Remove(iDictionaryObjRef, item);
        }

        public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value)
        {
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.TryGetValue(iDictionaryObjRef, null, key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace ABI.Windows.Foundation.Collections
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    internal static class IMapMethods<K, V>
    {
        // These function pointers will be set by IDictionaryMethods<K, KAbi, V, VAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal volatile unsafe static delegate*<IObjectReference, K, V> _Lookup;
        internal volatile unsafe static delegate*<IObjectReference, K, bool> _HasKey;
        internal volatile unsafe static delegate*<IObjectReference, global::System.Collections.Generic.IReadOnlyDictionary<K, V>> _GetView;
        internal volatile unsafe static delegate*<IObjectReference, K, V, bool> _Insert;
        internal volatile unsafe static delegate*<IObjectReference, K, void> _Remove;
        internal volatile static bool _RcwHelperInitialized;

        public static unsafe V Lookup(IObjectReference obj, K key)
        {
            return _Lookup(obj, key);
        }

        public static unsafe bool HasKey(IObjectReference obj, K key)
        {
            return _HasKey(obj, key);
        }

        public static unsafe global::System.Collections.Generic.IReadOnlyDictionary<K, V> GetView(IObjectReference obj)
        {
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return _GetView(obj);
            }

            if (_GetView != null)
            {
                return _GetView(obj);
            }
            else
            {
                var ThisPtr = obj.ThisPtr;
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[9](ThisPtr, &__retval));
                    return MarshalInterface<global::System.Collections.Generic.IReadOnlyDictionary<K, V>>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__retval);
                }
            }
        }

        public static unsafe bool Insert(IObjectReference obj, K key, V value)
        {
            return _Insert(obj, key, value);
        }

        public static unsafe void Remove(IObjectReference obj, K key)
        {
            _Remove(obj, key);
        }

        public static void Clear(IObjectReference obj)
        {
            _ClearHelper(obj);
        }

        private static unsafe void _ClearHelper(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[12](ThisPtr));
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
    using ABI.Windows.Foundation.Collections;
    using global::System.Runtime.CompilerServices;

#if EMBED
    internal
#else
    public
#endif
    static class IDictionaryMethods<K, V>
    {
        unsafe static IDictionaryMethods()
        {
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IDictionary<K, V>), typeof(global::ABI.System.Collections.Generic.IDictionary<K, V>));

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
                if (!IMapMethods<K, V>._RcwHelperInitialized)
                {
                    var initRcwHelperFallback = (Func<bool>)typeof(IDictionaryMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                        GetMethod("InitRcwHelperFallback", BindingFlags.NonPublic | BindingFlags.Static).
                        CreateDelegate(typeof(Func<bool>));
                    initRcwHelperFallback();
                }
            }
        }

        public static int get_Count(IObjectReference obj)
        {
            uint size = IMapMethods<K, V>.get_Size(obj);
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
            }
            return (int)size;
        }

        public static bool get_IsReadOnly(IObjectReference _) => false;

        public static void Add(IObjectReference obj, global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            IMapMethods<K, V>.Insert(obj, item.Key, item.Value);
        }

        public static void Clear(IObjectReference obj)
        {
            IMapMethods<K, V>.Clear(obj);
        }

        public static bool Contains(IObjectReference obj, Dictionary<K, (IntPtr, V)> __lookupCache, global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            bool hasKey = IMapMethods<K, V>.HasKey(obj, item.Key);
            if (!hasKey)
                return false;
            // todo: toctou
            V value = IMapMethods<K, V>.Lookup(obj, item.Key);
            return EqualityComparer<V>.Default.Equals(value, item.Value);
        }

        public static void CopyTo(IObjectReference obj, IObjectReference iEnumerableObjRef, global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && get_Count(obj) > 0)
                throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < get_Count(obj))
                throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);

            foreach (global::System.Collections.Generic.KeyValuePair<K, V> mapping in (new IEnumerableImpl<global::System.Collections.Generic.KeyValuePair<K, V>>(iEnumerableObjRef)))
            {
                array[arrayIndex++] = mapping;
            }
        }

        public static bool Remove(IObjectReference obj, global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            IMapMethods<K, V>.Remove(obj, item.Key);
            return true;
        }

        public static V Indexer_Get(IObjectReference obj, Dictionary<K, (IntPtr, V)> __lookupCache, K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return Lookup(obj, key);
        }

        public static void Indexer_Set(IObjectReference obj, K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            Insert(obj, key, value);
        }

        public static global::System.Collections.Generic.ICollection<K> get_Keys(IObjectReference obj) => new DictionaryKeyCollection(obj);

        public static global::System.Collections.Generic.ICollection<V> get_Values(IObjectReference obj) => new DictionaryValueCollection(obj);

        public static bool ContainsKey(IObjectReference obj, K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return IMapMethods<K, V>.HasKey(obj, key);
        }

        public static void Add(IObjectReference obj, K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (ContainsKey(obj, key))
                throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_AddingDuplicate);

            Insert(obj, key, value);
        }

        public static bool Remove(IObjectReference obj, K key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!IMapMethods<K, V>.HasKey(obj, key))
                return false;

            try
            {
                IMapMethods<K, V>.Remove(obj, key);
                return true;
            }
            catch (global::System.Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    return false;

                throw;
            }
        }

        public static bool TryGetValue(IObjectReference obj, Dictionary<K, (IntPtr, V)> __lookupCache, K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!IMapMethods<K, V>.HasKey(obj, key))
            {
                value = default!;
                return false;
            }

            try
            {
                value = Lookup(obj, key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default!;
                return false;
            }
        }

        private static V Lookup(IObjectReference obj, K key)
        {
            Debug.Assert(null != key);

            try
            {
                return IMapMethods<K, V>.Lookup(obj, key);
            }
            catch (global::System.Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(WinRTRuntimeErrorStrings.Arg_KeyNotFound);
                throw;
            }
        }

        private static bool Insert(IObjectReference obj, K key, V value)
        {
            Debug.Assert(null != key);

            bool replaced = IMapMethods<K, V>.Insert(obj, key, value);
            return replaced;
        }

        private sealed class DictionaryKeyCollection : global::System.Collections.Generic.ICollection<K>
        {
            private readonly IObjectReference iDictionaryObjRef;

            public DictionaryKeyCollection(IObjectReference iDictionaryObjRef)
            {
                if (iDictionaryObjRef == null)
                    throw new ArgumentNullException(nameof(iDictionaryObjRef));

                this.iDictionaryObjRef = iDictionaryObjRef;
            }

            private volatile IObjectReference __iEnumerableObjRef;
            private IObjectReference Make_IEnumerableObjRef()
            {
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iDictionaryObjRef.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.PIID), null);
                return __iEnumerableObjRef;
            }
            private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

            public void CopyTo(K[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (array.Length <= index && this.Count > 0)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Arg_IndexOutOfRangeException);
                if (array.Length - index < IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef))
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                int i = index;
                foreach (global::System.Collections.Generic.KeyValuePair<K, V> mapping in (new IEnumerableImpl<global::System.Collections.Generic.KeyValuePair<K, V>>(iEnumerableObjRef)))
                {
                    array[i++] = mapping.Key;
                }
            }

            public int Count => IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef);

            public bool IsReadOnly => true;

            void global::System.Collections.Generic.ICollection<K>.Add(K item)
            {
                throw new NotSupportedException(WinRTRuntimeErrorStrings.NotSupported_KeyCollectionSet);
            }

            void global::System.Collections.Generic.ICollection<K>.Clear()
            {
                throw new NotSupportedException(WinRTRuntimeErrorStrings.NotSupported_KeyCollectionSet);
            }

            public bool Contains(K item)
            {
                return IDictionaryMethods<K, V>.ContainsKey(iDictionaryObjRef, item);
            }

            bool global::System.Collections.Generic.ICollection<K>.Remove(K item)
            {
                throw new NotSupportedException(WinRTRuntimeErrorStrings.NotSupported_KeyCollectionSet);
            }

            global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            public global::System.Collections.Generic.IEnumerator<K> GetEnumerator() =>
                new DictionaryKeyEnumerator(iEnumerableObjRef);

            private sealed class DictionaryKeyEnumerator : global::System.Collections.Generic.IEnumerator<K>
            {
                private readonly IObjectReference iEnumerableObjRef;
                private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

                public DictionaryKeyEnumerator(IObjectReference iEnumerableObjRef)
                {
                    this.iEnumerableObjRef = iEnumerableObjRef;
                    enumeration = IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
                }

                public void Dispose()
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

        private sealed class DictionaryValueCollection : global::System.Collections.Generic.ICollection<V>
        {
            private readonly IObjectReference iDictionaryObjRef;

            public DictionaryValueCollection(IObjectReference iDictionaryObjRef)
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

            public void CopyTo(V[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (array.Length <= index && this.Count > 0)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Arg_IndexOutOfRangeException);
                if (array.Length - index < IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef))
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                int i = index;
                foreach (global::System.Collections.Generic.KeyValuePair<K, V> mapping in (new IEnumerableImpl<global::System.Collections.Generic.KeyValuePair<K, V>>(iEnumerableObjRef)))
                {
                    array[i++] = mapping.Value;
                }
            }

            public int Count => IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef);

            public bool IsReadOnly => true;

            void global::System.Collections.Generic.ICollection<V>.Add(V item)
            {
                throw new NotSupportedException(WinRTRuntimeErrorStrings.NotSupported_ValueCollectionSet);
            }

            void global::System.Collections.Generic.ICollection<V>.Clear()
            {
                throw new NotSupportedException(WinRTRuntimeErrorStrings.NotSupported_ValueCollectionSet);
            }

            public bool Contains(V item)
            {
                EqualityComparer<V> comparer = EqualityComparer<V>.Default;
                foreach (V value in this)
                    if (comparer.Equals(item, value))
                        return true;
                return false;
            }

            bool global::System.Collections.Generic.ICollection<V>.Remove(V item)
            {
                throw new NotSupportedException(WinRTRuntimeErrorStrings.NotSupported_ValueCollectionSet);
            }

            IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            public global::System.Collections.Generic.IEnumerator<V> GetEnumerator()
            {
                return new DictionaryValueEnumerator(iEnumerableObjRef);
            }

            private sealed class DictionaryValueEnumerator : global::System.Collections.Generic.IEnumerator<V>
            {
                private readonly IObjectReference iEnumerableObjRef;
                private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

                public DictionaryValueEnumerator(IObjectReference iEnumerableObjRef)
                {
                    this.iEnumerableObjRef = iEnumerableObjRef;
                    enumeration = IEnumerableMethods<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator(iEnumerableObjRef);
                }

                public void Dispose()
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

        internal static readonly Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IDictionary<K, V>));
        public static Guid IID => PIID;

        public static V Abi_Lookup_0(IntPtr thisPtr, K key)
        {
            return IDictionary<K, V>.FindAdapter(thisPtr).Lookup(key);
        }

        public static bool Abi_HasKey_2(IntPtr thisPtr, K key)
        {
            return IDictionary<K, V>.FindAdapter(thisPtr).HasKey(key);
        }

        public static global::System.Collections.Generic.IReadOnlyDictionary<K, V> Abi_GetView_3(IntPtr thisPtr)
        {
            return IDictionary<K, V>.FindAdapter(thisPtr).GetView();
        }

        public static bool Abi_Insert_4(IntPtr thisPtr, K key, V value)
        {
            return IDictionary<K, V>.FindAdapter(thisPtr).Insert(key, value);
        }

        public static void Abi_Remove_5(IntPtr thisPtr, K key)
        {
            IDictionary<K, V>.FindAdapter(thisPtr)._Remove(key);
        }

        public static void Abi_Clear_6(IntPtr thisPtr)
        {
            IDictionary<K, V>.FindAdapter(thisPtr).Clear();
        }

        public static uint Abi_get_Size_1(IntPtr thisPtr)
        {
            return IDictionary<K, V>.FindAdapter(thisPtr).Size;
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class IDictionaryMethods<K, KAbi, V, VAbi> where KAbi: unmanaged where VAbi: unmanaged
    {
        internal unsafe static delegate*<IObjectReference, K, V> _Lookup;
        internal unsafe static delegate*<IObjectReference, K, bool> _HasKey;
        internal unsafe static delegate*<IObjectReference, global::System.Collections.Generic.IReadOnlyDictionary<K, V>> _GetView;
        internal unsafe static delegate*<IObjectReference, K, V, bool> _Insert;
        internal unsafe static delegate*<IObjectReference, K, void> _Remove;

        public unsafe static bool InitRcwHelper(
            delegate*<IObjectReference, K, V> lookup,
            delegate*<IObjectReference, K, bool> hasKey,
            delegate*<IObjectReference, global::System.Collections.Generic.IReadOnlyDictionary<K, V>> getView,
            delegate*<IObjectReference, K, V, bool> insert,
            delegate*<IObjectReference, K, void> remove)
        {
            if (IMapMethods<K, V>._RcwHelperInitialized)
            {
                return true;
            }

            IMapMethods<K, V>._Lookup = lookup;
            IMapMethods<K, V>._HasKey = hasKey;
            IMapMethods<K, V>._GetView = getView;
            IMapMethods<K, V>._Insert = insert;
            IMapMethods<K, V>._Remove = remove;

            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.IDictionary<K, V>),
                IDictionaryImpl<K, V>.CreateRcw);
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IDictionary<K, V>), typeof(global::ABI.System.Collections.Generic.IDictionary<K, V>));

            IMapMethods<K, V>._RcwHelperInitialized = true;
            return true;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private unsafe static bool InitRcwHelperFallback()
        {
            return InitRcwHelper(
                &LookupDynamic,
                &HasKeyDynamic,
                null,
                &InsertDynamic,
                &RemoveDynamic);
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

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe bool InsertDynamic(IObjectReference obj, K key, V value)
        {
            var ThisPtr = obj.ThisPtr;
            object __key = default;
            object __value = default;
            byte replaced;
            var __params = new object[] { ThisPtr, null, null, (IntPtr)(void*)&replaced };
            try
            {
                __key = Marshaler<K>.CreateMarshaler2(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                __value = Marshaler<V>.CreateMarshaler2(value);
                __params[2] = Marshaler<V>.GetAbi(__value);
                DelegateHelper.Get(obj).Insert.DynamicInvokeAbi(__params);
                return replaced != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeMarshaler(__value);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe void RemoveDynamic(IObjectReference obj, K key)
        {
            var ThisPtr = obj.ThisPtr;
            object __key = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler2(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                DelegateHelper.Get(obj).Remove.DynamicInvokeAbi(__params);
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
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> getView,
            delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi, byte*, int> insert,
            delegate* unmanaged[Stdcall]<IntPtr, KAbi, int> remove,
            delegate* unmanaged[Stdcall]<IntPtr, int> clear)
        {
            if (IDictionaryMethods<K, V>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 7));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi*, int>*)abiToProjectionVftablePtr)[6] = lookup;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)abiToProjectionVftablePtr)[7] = getSize;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi, byte*, int>*)abiToProjectionVftablePtr)[8] = hasKey;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)abiToProjectionVftablePtr)[9] = getView;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi, byte*, int>*)abiToProjectionVftablePtr)[10] = insert;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi, int>*)abiToProjectionVftablePtr)[11] = remove;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)abiToProjectionVftablePtr)[12] = clear;

            if (!IDictionaryMethods<K, V>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
                return false;
            }

            // Register generic helper types referenced in CCW.
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>), typeof(global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>));

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
                global::System.Delegate.CreateDelegate(Lookup_0_Type, typeof(IDictionaryMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_Lookup_0), BindingFlags.NonPublic | BindingFlags.Static)),
                new _get_PropertyAsUInt32_Abi(Do_Abi_get_Size_1),
                global::System.Delegate.CreateDelegate(HasKey_2_Type, typeof(IDictionaryMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_HasKey_2), BindingFlags.NonPublic | BindingFlags.Static)),
                new IDictionary_Delegates.GetView_3_Abi(Do_Abi_GetView_3),
                global::System.Delegate.CreateDelegate(Insert_4_Type, typeof(IDictionaryMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_Insert_4), BindingFlags.NonPublic | BindingFlags.Static)),
                global::System.Delegate.CreateDelegate(Remove_5_Type, typeof(IDictionaryMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_Remove_5), BindingFlags.NonPublic | BindingFlags.Static)),
                new IDictionary_Delegates.Clear_6(Do_Abi_Clear_6)
            };

            InitCcw(
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi*, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[0]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint*, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[1]),
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi, byte*, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[2]),
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[3]),
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi, VAbi, byte*, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[4]),
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[5]),
                (delegate* unmanaged[Stdcall]<IntPtr, int>) Marshal.GetFunctionPointerForDelegate(DelegateCache[6])
            );
        }

        private static unsafe int Do_Abi_Lookup_0(void* thisPtr, KAbi key, VAbi* __return_value__)
        {
            V ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IDictionary<K, V>.FindAdapter(new IntPtr(thisPtr)).Lookup(Marshaler<K>.FromAbi(key));
                *__return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_HasKey_2(void* thisPtr, KAbi key, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IDictionary<K, V>.FindAdapter(new IntPtr(thisPtr)).HasKey(Marshaler<K>.FromAbi(key));
                *__return_value__ = (byte)(____return_value__ ? 1 : 0);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_GetView_3(IntPtr thisPtr, IntPtr* __return_value__)
        {
            global::System.Collections.Generic.IReadOnlyDictionary<K, V> ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IDictionary<K, V>.FindAdapter(thisPtr).GetView();
                *__return_value__ = MarshalInterface<global::System.Collections.Generic.IReadOnlyDictionary<K, V>>.FromManaged(____return_value__);

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_Insert_4(void* thisPtr, KAbi key, VAbi value, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IDictionary<K, V>.FindAdapter(new IntPtr(thisPtr)).Insert(Marshaler<K>.FromAbi(key), Marshaler<V>.FromAbi(value));
                *__return_value__ = (byte)(____return_value__ ? 1 : 0);
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_Remove_5(void* thisPtr, KAbi key)
        {
            try
            {
                IDictionary<K, V>.FindAdapter(new IntPtr(thisPtr))._Remove(Marshaler<K>.FromAbi(key));
            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_Clear_6(IntPtr thisPtr)
        {
            try
            {
                IDictionary<K, V>.FindAdapter(thisPtr).Clear();
            }
            catch (global::System.Exception __exception__)
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
                ____return_value__ = IDictionary<K,V>.FindAdapter(thisPtr).Size;
                *__return_value__ = ____return_value__;

            }
            catch (global::System.Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static global::System.Type _lookup_0_type;
        private static global::System.Type Lookup_0_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _lookup_0_type ?? MakeLookupType();
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static global::System.Type MakeLookupType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _lookup_0_type, Projections.GetAbiDelegateType(new global::System.Type[] { typeof(void*), typeof(KAbi), typeof(VAbi*), typeof(int) }), null);
            return _lookup_0_type;
        }

        private static global::System.Type _hasKey_2_type;
        private static global::System.Type HasKey_2_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _hasKey_2_type ?? MakeHasKeyType();
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static global::System.Type MakeHasKeyType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _hasKey_2_type, Projections.GetAbiDelegateType(new global::System.Type[] { typeof(void*), typeof(KAbi), typeof(byte*), typeof(int) }), null);
            return _hasKey_2_type;
        }

        private static global::System.Type _insert_4_type;
        private static global::System.Type Insert_4_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _insert_4_type ?? MakeInsertType();
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static global::System.Type MakeInsertType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _insert_4_type, Projections.GetAbiDelegateType(new global::System.Type[] { typeof(void*), typeof(KAbi), typeof(VAbi), typeof(byte*), typeof(int) }), null);
            return _insert_4_type;
        }

        private static global::System.Type _remove_5_type;
        private static global::System.Type Remove_5_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _remove_5_type ?? MakeRemoveType();
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static global::System.Type MakeRemoveType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _remove_5_type, Projections.GetAbiDelegateType(new global::System.Type[] { typeof(void*), typeof(KAbi), typeof(int) }), null);
            return _remove_5_type;
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

            private Delegate _insertDelegate;
            public Delegate Insert => _insertDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _insertDelegate, Insert_4_Type, 10);

            private Delegate _removeDelegate;
            public Delegate Remove => _removeDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _removeDelegate, Remove_5_Type, 11);

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
}

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    //This interface does not need to implement IMapView. Needs to be refactored
    [DynamicInterfaceCastableImplementation]
    [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
    interface IDictionary<K, V> : global::System.Collections.Generic.IDictionary<K, V>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IDictionary<K, V> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IDictionary<K, V> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IDictionary<K, V> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }

            if (!FeatureSwitches.EnableIDynamicInterfaceCastableSupport)
            {
                throw new NotSupportedException(
                    "'IDictionary<K, V>.FromAbi' relies on 'IDynamicInterfaceCastable' support, which is not currently " +
                    "available. Make sure the 'EnableIDynamicInterfaceCastableSupport' property is not set to 'false'.");
            }

            return (global::System.Collections.Generic.IDictionary<K, V>)(object)new IInspectable(ObjRefFromAbi(thisPtr));
        }

        public static IntPtr FromManaged(global::System.Collections.Generic.IDictionary<K, V> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IMap<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IDictionary<K, V>));

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IMap<K, V>
#pragma warning restore CA2257
        {
            private readonly global::System.Collections.Generic.IDictionary<K, V> _dictionary;

            public ToAbiHelper(global::System.Collections.Generic.IDictionary<K, V> dictionary) => _dictionary = dictionary;

            global::System.Collections.Generic.IEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>> global::Windows.Foundation.Collections.IIterable<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.First() =>
                new KeyValuePair<K, V>.Enumerator(_dictionary.GetEnumerator());

            public V Lookup(K key)
            {
                V value;
                bool keyFound = _dictionary.TryGetValue(key, out value);

                if (!keyFound)
                {
                    Debug.Assert(key != null);
                    Exception e = new KeyNotFoundException(String.Format(WinRTRuntimeErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }

                return value;
            }

            public uint Size { get => (uint)_dictionary.Count; }

            public bool HasKey(K key) => _dictionary.ContainsKey(key);

            global::System.Collections.Generic.IReadOnlyDictionary<K, V> global::Windows.Foundation.Collections.IMap<K, V>.GetView()
            {
                if (!(_dictionary is global::System.Collections.Generic.IReadOnlyDictionary<K, V> roDictionary))
                {
                    roDictionary = new ReadOnlyDictionary<K, V>(_dictionary);
                }
                return roDictionary;
            }

            public bool Insert(K key, V value)
            {
                bool replacing = _dictionary.ContainsKey(key);
                _dictionary[key] = value;
                return replacing;
            }

            public void _Remove(K key)
            {
                bool removed = _dictionary.Remove(key);

                if (!removed)
                {
                    Debug.Assert(key != null);
                    Exception e = new KeyNotFoundException(String.Format(WinRTRuntimeErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }
            }

            public void Clear() => _dictionary.Clear();
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        
        static IDictionary()
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
                    if (IDictionaryMethods<K, V>.AbiToProjectionVftablePtr == default)
                    {
                        // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                        var initFallbackCCWVtable = (Action)typeof(IDictionaryMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                            GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                            CreateDelegate(typeof(Action));
                        initFallbackCCWVtable();
                    }
                }
            }

            AbiToProjectionVftablePtr = IDictionaryMethods<K, V>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public unsafe struct Vftbl
#pragma warning restore CA2257
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.IDictionary<K, V>.AbiToProjectionVftablePtr;

            public static readonly Guid PIID = ABI.System.Collections.Generic.IDictionary<K, V>.PIID;
        }

        private static readonly ConditionalWeakTable<global::System.Collections.Generic.IDictionary<K, V>, ToAbiHelper> _adapterTable = new();

        internal static global::Windows.Foundation.Collections.IMap<K, V> FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IDictionary<K, V>>(thisPtr);
            return _adapterTable.GetValue(__this, (dictionary) => new ToAbiHelper(dictionary));
        }

        public static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, PIID);
        }

        public static readonly Guid PIID = IDictionaryMethods<K,V>.PIID;

        global::System.Collections.Generic.ICollection<K> global::System.Collections.Generic.IDictionary<K, V>.Keys
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
                return IDictionaryMethods<K, V>.get_Keys(_obj);
            }
        }

        global::System.Collections.Generic.ICollection<V> global::System.Collections.Generic.IDictionary<K, V>.Values
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
                return IDictionaryMethods<K, V>.get_Values(_obj);
            }
        }

        int global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Count
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
                return IDictionaryMethods<K, V>.get_Count(_obj);
            }
        }

        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.IsReadOnly
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
                return IDictionaryMethods<K, V>.get_IsReadOnly(_obj);
            }
        }

        V global::System.Collections.Generic.IDictionary<K, V>.this[K key] 
        { 
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
                return IDictionaryMethods<K, V>.Indexer_Get(_obj, null, key);
            }
            set
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
                IDictionaryMethods<K, V>.Indexer_Set(_obj, key, value);
            }
        }

        void global::System.Collections.Generic.IDictionary<K, V>.Add(K key, V value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            IDictionaryMethods<K, V>.Add(_obj, key, value);   
        }

        bool global::System.Collections.Generic.IDictionary<K, V>.ContainsKey(K key)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            return IDictionaryMethods<K, V>.ContainsKey(_obj, key);
        }

        bool global::System.Collections.Generic.IDictionary<K, V>.Remove(K key)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            return IDictionaryMethods<K, V>.Remove(_obj, key);
        }

        bool global::System.Collections.Generic.IDictionary<K, V>.TryGetValue(K key, out V value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            return IDictionaryMethods<K, V>.TryGetValue(_obj, null, key, out value);
        }

        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Add(global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            IDictionaryMethods<K, V>.Add(_obj, item);
        }

        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Contains(global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            return IDictionaryMethods<K, V>.Contains(_obj, null, item);
        }

        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.CopyTo(global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            ((IWinRTObject)this).IsInterfaceImplemented(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle, true);
            var _objEnumerable = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle);
            IDictionaryMethods<K, V>.CopyTo(_obj, _objEnumerable, array, arrayIndex);
        }

        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Remove(global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            return IDictionaryMethods<K, V>.Remove(_obj, item);
        }

        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Clear()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle);
            IDictionaryMethods<K, V>.Clear(_obj);
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
    static class IDictionary_Delegates
    {
        public unsafe delegate int GetView_3(IntPtr thisPtr, out IntPtr __return_value__);
        internal unsafe delegate int GetView_3_Abi(IntPtr thisPtr, IntPtr* __return_value__);
        public unsafe delegate int Clear_6(IntPtr thisPtr);
    }
}
