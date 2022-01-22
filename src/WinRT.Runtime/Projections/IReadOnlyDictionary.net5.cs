// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        private IObjectReference _inner;

        internal IReadOnlyDictionaryImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        private volatile IObjectReference __iReadOnlyDictionaryObjRef;
        private IObjectReference Make_IDictionaryObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iReadOnlyDictionaryObjRef, _inner.As<ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.Vftbl>(), null);
            return __iReadOnlyDictionaryObjRef;
        }
        private IObjectReference iReadOnlyDictionaryObjRef => __iReadOnlyDictionaryObjRef ?? Make_IDictionaryObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>.Vftbl>(), null);
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
        public static unsafe V Lookup(IObjectReference obj, K key)
        {
            var _obj = (ObjectReference<global::ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;

            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Lookup_0.DynamicInvokeAbi(__params);
                return Marshaler<V>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeAbi(__params[2]);
            }
        }

        public static unsafe bool HasKey(IObjectReference obj, K key)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.Vftbl>)obj; 
            var ThisPtr = _obj.ThisPtr;

            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.HasKey_2.DynamicInvokeAbi(__params);
                return (byte)__params[2] != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
            }
        }

        public static unsafe void Split(IObjectReference obj, out global::Windows.Foundation.Collections.IMapView<K, V> first, out global::Windows.Foundation.Collections.IMapView<K, V> second)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;

            IntPtr __first = default;
            IntPtr __second = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Split_3(ThisPtr, out __first, out __second));
                first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__first);
                second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromAbi(__second);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__first);
                MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__second);
            }
        }

        public static unsafe uint get_Size(IObjectReference obj)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;

            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSize_1(ThisPtr, out __retval));
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
        public static int get_Count(IObjectReference obj)
        {
            uint size = ABI.Windows.Foundation.Collections.IMapViewMethods<K, V>.get_Size(obj);

            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
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
                    throw new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
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
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iReadOnlyDictionaryObjRef.As<ABI.System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<K, V>>.Vftbl>(), null);
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
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iDictionaryObjRef.As<ABI.System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<K, V>>.Vftbl>(), null);
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
    }

    

    [DynamicInterfaceCastableImplementation]
    [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
    interface IReadOnlyDictionary<K, V> : global::System.Collections.Generic.IReadOnlyDictionary<K, V>, global::Windows.Foundation.Collections.IMapView<K, V>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyDictionary<K, V> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<Vftbl>(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IReadOnlyDictionary<K, V> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReadOnlyDictionary<K, V>));

        private sealed class ReadOnlyDictionaryKeyCollection : global::System.Collections.Generic.IEnumerable<K>
        {
            private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary;

            public ReadOnlyDictionaryKeyCollection(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
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
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
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

        private sealed class ReadOnlyDictionaryValueCollection : global::System.Collections.Generic.IEnumerable<V>
        {
            private readonly global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary;

            public ReadOnlyDictionaryValueCollection(global::System.Collections.Generic.IReadOnlyDictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException(nameof(dictionary));

                this.dictionary = dictionary;
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
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
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

        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IMapView<K, V>
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
                    Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
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

                if (!(_dictionary is ConstantSplittableMap splittableMap))
                    splittableMap = new ConstantSplittableMap(_dictionary);

                splittableMap.Split(out first, out second);
            }

            private sealed class ConstantSplittableMap : global::Windows.Foundation.Collections.IMapView<K, V>, global::System.Collections.Generic.IReadOnlyDictionary<K, V>
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
                    get => new ReadOnlyDictionaryKeyCollection(this);
                }

                public global::System.Collections.Generic.IEnumerable<V> Values
                {
                    get => new ReadOnlyDictionaryValueCollection(this);
                }

                public int Count => lastItemIndex - firstItemIndex + 1;

                public V this[K key] => Lookup(key);

                public V Lookup(K key)
                {
                    V value;
                    bool found = TryGetValue(key, out value);

                    if (!found)
                    {
                        Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
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

                    firstPartition = new ConstantSplittableMap(items, firstItemIndex, pivot);
                    secondPartition = new ConstantSplittableMap(items, pivot + 1, lastItemIndex);
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
                    return new Enumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>(itemsAsIKeyValuePairs, firstItemIndex, lastItemIndex);
                }

                public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> GetEnumerator()
                {
                    return new Enumerator<global::System.Collections.Generic.KeyValuePair<K, V>>(items, firstItemIndex, lastItemIndex);
                }

                IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
                {
                    return new Enumerator<global::System.Collections.Generic.KeyValuePair<K, V>>(items, firstItemIndex, lastItemIndex);
                }
            }

            internal struct Enumerator<T> : global::System.Collections.Generic.IEnumerator<T>
            {
                private readonly T[] _array;
                private readonly int _start;
                private readonly int _end;
                private int _current;

                internal Enumerator(T[] items, int first, int end)
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
                        if (_current < _start) throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumNotStarted);
                        if (_current > _end) throw new InvalidOperationException(ErrorStrings.InvalidOperation_EnumEnded);
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
        }

        [Guid("E480CE40-A338-4ADA-ADCF-272272E48CB9")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate Lookup_0;
            private void* _get_Size_1;
            internal delegate* unmanaged[Stdcall]<IntPtr, out uint, int> GetSize_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)_get_Size_1; set => _get_Size_1 = (void*)value; }
            public global::System.Delegate HasKey_2;
            private void* _split_3;
            internal delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, out IntPtr, int> Split_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, out IntPtr, int>)_split_3; set => _split_3 = (void*)value; }

            public static Guid PIID = GuidGenerator.CreateIID(typeof(IReadOnlyDictionary<K, V>));
            private static readonly Type Lookup_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, Marshaler<V>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type HasKey_2_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, typeof(byte).MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr) : this()
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                Lookup_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], Lookup_0_Type);
                GetSize_1 = (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)vftbl[7];
                HasKey_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], HasKey_2_Type);
                Split_3 = (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, out IntPtr, int>)vftbl[9];
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            private static readonly Delegate[] DelegateCache = new Delegate[2];

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    Lookup_0 = global::System.Delegate.CreateDelegate(Lookup_0_Type, typeof(Vftbl).GetMethod("Do_Abi_Lookup_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType, Marshaler<V>.AbiType)),
                    _get_Size_1 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new _get_PropertyAsUInt32(Do_Abi_get_Size_1)),
                    HasKey_2 = global::System.Delegate.CreateDelegate(HasKey_2_Type, typeof(Vftbl).GetMethod("Do_Abi_HasKey_2", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    _split_3 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IReadOnlyDictionary_Delegates.Split_3(Do_Abi_Split_3)),
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Lookup_0);
                nativeVftbl[7] = (IntPtr)AbiToProjectionVftable.GetSize_1;
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.HasKey_2);
                nativeVftbl[9] = (IntPtr)AbiToProjectionVftable.Split_3;

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyDictionary<K, V>, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyDictionary<K, V>, ToAbiHelper>();

            private static global::Windows.Foundation.Collections.IMapView<K, V> FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IReadOnlyDictionary<K, V>>(thisPtr);
                return _adapterTable.GetValue(__this, (dictionary) => new ToAbiHelper(dictionary));
            }

            private static unsafe int Do_Abi_Lookup_0<KAbi, VAbi>(void* thisPtr, KAbi key, out VAbi __return_value__)
            {
                V ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).Lookup(Marshaler<K>.FromAbi(key));
                    __return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_HasKey_2<KAbi>(void* thisPtr, KAbi key, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).HasKey(Marshaler<K>.FromAbi(key));
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Split_3(IntPtr thisPtr, out IntPtr first, out IntPtr second)
            {

                first = default;
                second = default;
                global::Windows.Foundation.Collections.IMapView<K, V> __first = default;
                global::Windows.Foundation.Collections.IMapView<K, V> __second = default;

                try
                {
                    FindAdapter(thisPtr).Split(out __first, out __second);
                    first = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__first);
                    second = MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.FromManaged(__second);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).Size;
                    __return_value__ = ____return_value__;
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        global::System.Collections.Generic.IEnumerable<K> global::System.Collections.Generic.IReadOnlyDictionary<K, V>.Keys
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle));
                return IReadOnlyDictionaryMethods<K, V>.get_Keys(_obj);
            }
        }
        global::System.Collections.Generic.IEnumerable<V> global::System.Collections.Generic.IReadOnlyDictionary<K, V>.Values
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle));
                return IReadOnlyDictionaryMethods<K, V>.get_Values(_obj);
            }
        }
        int global::System.Collections.Generic.IReadOnlyCollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Count
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle));
                return IReadOnlyDictionaryMethods<K, V>.get_Count(_obj);
            }
        }
        V global::System.Collections.Generic.IReadOnlyDictionary<K, V>.this[K key]
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle));
                return IReadOnlyDictionaryMethods<K, V>.Indexer_Get(_obj, key);
            }
        }
        bool global::System.Collections.Generic.IReadOnlyDictionary<K, V>.ContainsKey(K key)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle));
            return IReadOnlyDictionaryMethods<K, V>.ContainsKey(_obj, key);
        }
        bool global::System.Collections.Generic.IReadOnlyDictionary<K, V>.TryGetValue(K key, out V value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyDictionary<K, V>).TypeHandle));
            return IReadOnlyDictionaryMethods<K, V>.TryGetValue(_obj, key, out value);
        }
        global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator()
        {
            ((IWinRTObject)this).IsInterfaceImplemented(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle, true);
            var _objEnumerable = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle));
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
    }
}
