// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    internal sealed class IDictionaryImpl<K, V> : IDictionary<K, V>
    {
        private IObjectReference _inner;
        private Dictionary<K, (IntPtr, V)> _lookupCache;

        internal IDictionaryImpl(IObjectReference _inner)
        {
            this._inner = _inner;
            this._lookupCache = new Dictionary<K, (IntPtr, V)>();
        }

        private volatile IObjectReference __iDictionaryObjRef;
        private IObjectReference Make_IDictionaryObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iDictionaryObjRef, _inner.As<ABI.System.Collections.Generic.IDictionary<K, V>.Vftbl>(), null);
            return __iDictionaryObjRef;
        }
        private IObjectReference iDictionaryObjRef => __iDictionaryObjRef ?? Make_IDictionaryObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>.Vftbl>(), null);
            return __iEnumerableObjRef;
        }
        private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

        public V this[K key] 
        { 
            get => ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Indexer_Get(iDictionaryObjRef, _lookupCache, key);
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
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.Contains(iDictionaryObjRef, _lookupCache, item);
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
            return ABI.System.Collections.Generic.IDictionaryMethods<K, V>.TryGetValue(iDictionaryObjRef, _lookupCache, key, out value);
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
    using ABI.System.Collections.Generic;

    internal static class IMapMethods<K, V>
    {
        public static unsafe V Lookup(IObjectReference obj, Dictionary<K, (IntPtr, V)> __lookupCache, K key)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __key = default;
            var __params = new object[] { ThisPtr, null, null }; 
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Lookup_0.DynamicInvokeAbi(__params);

                if (__lookupCache != null && __lookupCache.TryGetValue(key, out var __cachedRcw) && __cachedRcw.Item1 == (IntPtr)__params[2])
                {
                    return __cachedRcw.Item2;
                }
                else
                {
                    var value = Marshaler<V>.FromAbi(__params[2]);
                    if (__lookupCache != null)
                    {
                        __lookupCache[key] = ((IntPtr)__params[2], value);
                    }
                    return value;
                }
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeAbi(__params[2]);
            }
        }

        public static unsafe bool HasKey(IObjectReference obj, K key)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
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

        public static unsafe global::System.Collections.Generic.IReadOnlyDictionary<K, V> GetView(IObjectReference obj)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_3(ThisPtr, out __retval));
                return MarshalInterface<global::System.Collections.Generic.IReadOnlyDictionary<K, V>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IMapView<K, V>>.DisposeAbi(__retval);
            }
        }

        public static unsafe bool Insert(IObjectReference obj, K key, V value)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __key = default;
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                __value = Marshaler<V>.CreateMarshaler(value);
                __params[2] = Marshaler<V>.GetAbi(__value);
                _obj.Vftbl.Insert_4.DynamicInvokeAbi(__params);
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeMarshaler(__value);
            }
        }

        public static unsafe void Remove(IObjectReference obj, K key)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __key = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Remove_5.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
            }
        }

        public static void Clear(IObjectReference obj)
        {
            _ClearHelper(obj);
        }

        private static unsafe void _ClearHelper(IObjectReference obj)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_6(ThisPtr));
        }

        public static unsafe uint get_Size(IObjectReference obj)
        {
            var _obj = (ObjectReference<IDictionary<K, V>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
            return __retval;
        }
    }

}

namespace ABI.System.Collections.Generic
{
    using ABI.Windows.Foundation.Collections;

    public static class IDictionaryMethods<K, V>
    {
        public static int get_Count(IObjectReference obj)
        {
            uint size = IMapMethods<K, V>.get_Size(obj);
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingDictionaryTooLarge);
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
            V value = IMapMethods<K, V>.Lookup(obj, __lookupCache, item.Key);
            return EqualityComparer<V>.Default.Equals(value, item.Value);
        }

        public static void CopyTo(IObjectReference obj, IObjectReference iEnumerableObjRef, global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && get_Count(obj) > 0)
                throw new ArgumentException(ErrorStrings.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < get_Count(obj))
                throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

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
            return Lookup(obj, __lookupCache, key);
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
                throw new ArgumentException(ErrorStrings.Argument_AddingDuplicate);

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
                value = Lookup(obj, __lookupCache, key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default!;
                return false;
            }
        }

        private static V Lookup(IObjectReference obj, Dictionary<K, (IntPtr, V)> __lookupCache, K key)
        {
            Debug.Assert(null != key);

            try
            {
                return IMapMethods<K, V>.Lookup(obj, __lookupCache, key);
            }
            catch (global::System.Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new KeyNotFoundException(ErrorStrings.Arg_KeyNotFound);
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
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iDictionaryObjRef.As<ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>.Vftbl>(), null);
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
                    throw new ArgumentException(ErrorStrings.Arg_IndexOutOfRangeException);
                if (array.Length - index < IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef))
                    throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

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
                throw new NotSupportedException(ErrorStrings.NotSupported_KeyCollectionSet);
            }

            void global::System.Collections.Generic.ICollection<K>.Clear()
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_KeyCollectionSet);
            }

            public bool Contains(K item)
            {
                return IDictionaryMethods<K, V>.ContainsKey(iDictionaryObjRef, item);
            }

            bool global::System.Collections.Generic.ICollection<K>.Remove(K item)
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_KeyCollectionSet);
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
                global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, iDictionaryObjRef.As<ABI.System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>.Vftbl>(), null);
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
                    throw new ArgumentException(ErrorStrings.Arg_IndexOutOfRangeException);
                if (array.Length - index < IDictionaryMethods<K, V>.get_Count(iDictionaryObjRef))
                    throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

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
                throw new NotSupportedException(ErrorStrings.NotSupported_ValueCollectionSet);
            }

            void global::System.Collections.Generic.ICollection<V>.Clear()
            {
                throw new NotSupportedException(ErrorStrings.NotSupported_ValueCollectionSet);
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
                throw new NotSupportedException(ErrorStrings.NotSupported_ValueCollectionSet);
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
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<Vftbl>(obj, GuidGenerator.GetIID(typeof(IDictionary<K, V>)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IDictionary<K, V> FromAbi(IntPtr thisPtr) =>
            thisPtr == IntPtr.Zero ? null : (global::System.Collections.Generic.IDictionary<K, V>)(object)new IInspectable(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IDictionary<K, V> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IMap<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IDictionary<K, V>));

        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IMap<K, V>
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
                    Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
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
                    Exception e = new KeyNotFoundException(ErrorStrings.Format(ErrorStrings.Arg_KeyNotFoundWithKey, key.ToString()));
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }
            }

            public void Clear() => _dictionary.Clear();
        }

        [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate Lookup_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public global::System.Delegate HasKey_2;
            public IDictionary_Delegates.GetView_3 GetView_3;
            public global::System.Delegate Insert_4;
            public global::System.Delegate Remove_5;
            public IDictionary_Delegates.Clear_6 Clear_6;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(IDictionary<K, V>));
            private static readonly Type Lookup_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, Marshaler<V>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type HasKey_2_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type Insert_4_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, Marshaler<V>.AbiType, typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type Remove_5_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType, typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                Lookup_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], Lookup_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                HasKey_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], HasKey_2_Type);
                GetView_3 = Marshal.GetDelegateForFunctionPointer<IDictionary_Delegates.GetView_3>(vftbl[9]);
                Insert_4 = Marshal.GetDelegateForFunctionPointer(vftbl[10], Insert_4_Type);
                Remove_5 = Marshal.GetDelegateForFunctionPointer(vftbl[11], Remove_5_Type);
                Clear_6 = Marshal.GetDelegateForFunctionPointer<IDictionary_Delegates.Clear_6>(vftbl[12]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    Lookup_0 = global::System.Delegate.CreateDelegate(Lookup_0_Type, typeof(Vftbl).GetMethod("Do_Abi_Lookup_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType, Marshaler<V>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    HasKey_2 = global::System.Delegate.CreateDelegate(HasKey_2_Type, typeof(Vftbl).GetMethod("Do_Abi_HasKey_2", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    GetView_3 = Do_Abi_GetView_3,
                    Insert_4 = global::System.Delegate.CreateDelegate(Insert_4_Type, typeof(Vftbl).GetMethod("Do_Abi_Insert_4", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType, Marshaler<V>.AbiType)),
                    Remove_5 = global::System.Delegate.CreateDelegate(Remove_5_Type, typeof(Vftbl).GetMethod("Do_Abi_Remove_5", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    Clear_6 = Do_Abi_Clear_6
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 7);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Lookup_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.HasKey_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetView_3);
                nativeVftbl[10] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Insert_4);
                nativeVftbl[11] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Remove_5);
                nativeVftbl[12] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Clear_6);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.Generic.IDictionary<K, V>, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.Generic.IDictionary<K, V>, ToAbiHelper>();

            private static global::Windows.Foundation.Collections.IMap<K, V> FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IDictionary<K, V>>(thisPtr);
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
            private static unsafe int Do_Abi_GetView_3(IntPtr thisPtr, out IntPtr __return_value__)
            {
                global::System.Collections.Generic.IReadOnlyDictionary<K, V> ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).GetView();
                    __return_value__ = MarshalInterface<global::System.Collections.Generic.IReadOnlyDictionary<K, V>>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Insert_4<KAbi, VAbi>(void* thisPtr, KAbi key, VAbi value, out byte __return_value__)
            {
                bool ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).Insert(Marshaler<K>.FromAbi(key), Marshaler<V>.FromAbi(value));
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Remove_5<KAbi>(void* thisPtr, KAbi key)
            {


                try
                {
                    FindAdapter(new IntPtr(thisPtr))._Remove(Marshaler<K>.FromAbi(key));
                }
                catch (Exception __exception__)
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
                    FindAdapter(thisPtr).Clear();
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
                    ____return_value__ = FindAdapter(thisPtr).Size; __return_value__ = ____return_value__;

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

        global::System.Collections.Generic.ICollection<K> global::System.Collections.Generic.IDictionary<K, V>.Keys
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                return IDictionaryMethods<K, V>.get_Keys(_obj);
            }
        }

        global::System.Collections.Generic.ICollection<V> global::System.Collections.Generic.IDictionary<K, V>.Values
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                return IDictionaryMethods<K, V>.get_Values(_obj);
            }
        }

        int global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Count
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                return IDictionaryMethods<K, V>.get_Count(_obj);
            }
        }

        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.IsReadOnly
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                return IDictionaryMethods<K, V>.get_IsReadOnly(_obj);
            }
        }

        V global::System.Collections.Generic.IDictionary<K, V>.this[K key] 
        { 
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                return IDictionaryMethods<K, V>.Indexer_Get(_obj, GetLookupCache(), key);
            }
            set
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                IDictionaryMethods<K, V>.Indexer_Set(_obj, key, value);
            }
        }

        void global::System.Collections.Generic.IDictionary<K, V>.Add(K key, V value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            IDictionaryMethods<K, V>.Add(_obj, key, value);   
        }

        bool global::System.Collections.Generic.IDictionary<K, V>.ContainsKey(K key)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            return IDictionaryMethods<K, V>.ContainsKey(_obj, key);
        }

        bool global::System.Collections.Generic.IDictionary<K, V>.Remove(K key)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            return IDictionaryMethods<K, V>.Remove(_obj, key);
        }

        Dictionary<K, (IntPtr, V)> GetLookupCache()
        {
            return (Dictionary<K, (IntPtr, V)>)((IWinRTObject)this).GetOrCreateTypeHelperData(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle,
                () => new Dictionary<K, (IntPtr, V)>());
        }

        bool global::System.Collections.Generic.IDictionary<K, V>.TryGetValue(K key, out V value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            return IDictionaryMethods<K, V>.TryGetValue(_obj, GetLookupCache(), key, out value);
        }

        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Add(global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            IDictionaryMethods<K, V>.Add(_obj, item);
        }

        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Contains(global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            return IDictionaryMethods<K, V>.Contains(_obj, GetLookupCache(), item);
        }

        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.CopyTo(global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            ((IWinRTObject)this).IsInterfaceImplemented(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle, true);
            var _objEnumerable = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<KeyValuePair<K, V>>).TypeHandle));
            IDictionaryMethods<K, V>.CopyTo(_obj, _objEnumerable, array, arrayIndex);
        }

        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Remove(global::System.Collections.Generic.KeyValuePair<K, V> item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            return IDictionaryMethods<K, V>.Remove(_obj, item);
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
    static class IDictionary_Delegates
    {
        public unsafe delegate int GetView_3(IntPtr thisPtr, out IntPtr __return_value__);
        public unsafe delegate int Clear_6(IntPtr thisPtr);
    }
}
