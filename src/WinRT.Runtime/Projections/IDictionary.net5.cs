// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

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

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    //This interface does not need to implement IMapView. Needs to be refactored
    [DynamicInterfaceCastableImplementation]
    [Guid("3C2925FE-8519-45C1-AA79-197B6718C1C1")]
    interface IDictionary<K, V> : global::System.Collections.Generic.IDictionary<K, V>, global::Windows.Foundation.Collections.IMap<K, V>
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

        public sealed class FromAbiHelper : global::System.Collections.Generic.IDictionary<K, V>
        {
            private readonly global::Windows.Foundation.Collections.IMap<K, V> _map;
            internal readonly Dictionary<K, (IntPtr, V)> _lookupCache = new();

            public FromAbiHelper(global::Windows.Foundation.Collections.IMap<K, V> map)
            {
                _map = map;
            }

            public int Count
            {
                get
                {
                    uint size = _map.Size;

                    if (((uint)int.MaxValue) < size)
                    {
                        throw new InvalidOperationException(WinRTRuntimeLocalizationResources.InvalidOperation_CollectionBackingDictionaryTooLarge);
                    }

                    return (int)size;
                }
            }

            public bool IsReadOnly { get => false; }

            public void Add(global::System.Collections.Generic.KeyValuePair<K, V> item)
            {
                _map.Insert(item.Key, item.Value);
            }

            public void Clear()
            {
                _map.Clear();
            }

            public bool Contains(global::System.Collections.Generic.KeyValuePair<K, V> item)
            {
                bool hasKey = _map.HasKey(item.Key);
                if (!hasKey)
                    return false;
                // todo: toctou
                V value = _map.Lookup(item.Key);
                return EqualityComparer<V>.Default.Equals(value, item.Value);
            }

            public void CopyTo(global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                if (array.Length <= arrayIndex && Count > 0)
                    throw new ArgumentException(WinRTRuntimeLocalizationResources.Argument_IndexOutOfArrayBounds);

                if (array.Length - arrayIndex < Count)
                    throw new ArgumentException(WinRTRuntimeLocalizationResources.Argument_InsufficientSpaceToCopyCollection);

                foreach (global::System.Collections.Generic.KeyValuePair<K, V> mapping in this)
                {
                    array[arrayIndex++] = mapping;
                }
            }

            public bool Remove(global::System.Collections.Generic.KeyValuePair<K, V> item)
            {
                _map._Remove(item.Key);
                return true;
            }

            public V this[K key] { get => Indexer_Get(key); set => Indexer_Set(key, value); }

            private V Indexer_Get(K key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                return Lookup(_map, key);
            }
            private void Indexer_Set(K key, V value)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                Insert(_map, key, value);
            }

            public global::System.Collections.Generic.ICollection<K> Keys { get => new DictionaryKeyCollection(this); }

            public global::System.Collections.Generic.ICollection<V> Values { get => new DictionaryValueCollection(this); }

            public bool ContainsKey(K key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));
                return _map.HasKey(key);
            }

            public void Add(K key, V value)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (ContainsKey(key))
                    throw new ArgumentException(WinRTRuntimeLocalizationResources.Argument_AddingDuplicate);

                Insert(_map, key, value);
            }

            public bool Remove(K key)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (!_map.HasKey(key))
                    return false;

                try
                {
                    _map._Remove(key);
                    return true;
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        return false;

                    throw;
                }
            }

            public bool TryGetValue(K key, out V value)
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (!_map.HasKey(key))
                {
                    value = default!;
                    return false;
                }

                try
                {
                    value = Lookup(_map, key);
                    return true;
                }
                catch (KeyNotFoundException)
                {
                    value = default!;
                    return false;
                }
            }

            private static V Lookup(global::Windows.Foundation.Collections.IMap<K, V> _this, K key)
            {
                Debug.Assert(null != key);

                try
                {
                    return _this.Lookup(key);
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        throw new KeyNotFoundException(WinRTRuntimeLocalizationResources.Arg_KeyNotFound);
                    throw;
                }
            }

            private static bool Insert(global::Windows.Foundation.Collections.IMap<K, V> _this, K key, V value)
            {
                Debug.Assert(null != key);

                bool replaced = _this.Insert(key, value);
                return replaced;
            }

            public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> GetEnumerator() => ((global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<K, V>>)(IWinRTObject)_map).GetEnumerator();

            global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class DictionaryKeyCollection : global::System.Collections.Generic.ICollection<K>
            {
                private readonly global::System.Collections.Generic.IDictionary<K, V> dictionary;

                public DictionaryKeyCollection(global::System.Collections.Generic.IDictionary<K, V> dictionary)
                {
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
                }

                public void CopyTo(K[] array, int index)
                {
                    if (array == null)
                        throw new ArgumentNullException(nameof(array));
                    if (index < 0)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    if (array.Length <= index && this.Count > 0)
                        throw new ArgumentException(WinRTRuntimeLocalizationResources.Arg_IndexOutOfRangeException);
                    if (array.Length - index < dictionary.Count)
                        throw new ArgumentException(WinRTRuntimeLocalizationResources.Argument_InsufficientSpaceToCopyCollection);

                    int i = index;
                    foreach (global::System.Collections.Generic.KeyValuePair<K, V> mapping in dictionary)
                    {
                        array[i++] = mapping.Key;
                    }
                }

                public int Count => dictionary.Count;

                public bool IsReadOnly => true;

                void global::System.Collections.Generic.ICollection<K>.Add(K item)
                {
                    throw new NotSupportedException(WinRTRuntimeLocalizationResources.NotSupported_KeyCollectionSet);
                }

                void global::System.Collections.Generic.ICollection<K>.Clear()
                {
                    throw new NotSupportedException(WinRTRuntimeLocalizationResources.NotSupported_KeyCollectionSet);
                }

                public bool Contains(K item)
                {
                    return dictionary.ContainsKey(item);
                }

                bool global::System.Collections.Generic.ICollection<K>.Remove(K item)
                {
                    throw new NotSupportedException(WinRTRuntimeLocalizationResources.NotSupported_KeyCollectionSet);
                }

                global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

                public global::System.Collections.Generic.IEnumerator<K> GetEnumerator() =>
                    new DictionaryKeyEnumerator(dictionary);

                private sealed class DictionaryKeyEnumerator : global::System.Collections.Generic.IEnumerator<K>
                {
                    private readonly global::System.Collections.Generic.IDictionary<K, V> dictionary;
                    private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

                    public DictionaryKeyEnumerator(global::System.Collections.Generic.IDictionary<K, V> dictionary)
                    {
                        if (dictionary == null)
                            throw new ArgumentNullException(nameof(dictionary));

                        this.dictionary = dictionary;
                        enumeration = dictionary.GetEnumerator();
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
                        enumeration = dictionary.GetEnumerator();
                    }
                }
            }

            private sealed class DictionaryValueCollection : global::System.Collections.Generic.ICollection<V>
            {
                private readonly global::System.Collections.Generic.IDictionary<K, V> dictionary;

                public DictionaryValueCollection(global::System.Collections.Generic.IDictionary<K, V> dictionary)
                {
                    if (dictionary == null)
                        throw new ArgumentNullException(nameof(dictionary));

                    this.dictionary = dictionary;
                }

                public void CopyTo(V[] array, int index)
                {
                    if (array == null)
                        throw new ArgumentNullException(nameof(array));
                    if (index < 0)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    if (array.Length <= index && this.Count > 0)
                        throw new ArgumentException(WinRTRuntimeLocalizationResources.Arg_IndexOutOfRangeException);
                    if (array.Length - index < dictionary.Count)
                        throw new ArgumentException(WinRTRuntimeLocalizationResources.Argument_InsufficientSpaceToCopyCollection);

                    int i = index;
                    foreach (global::System.Collections.Generic.KeyValuePair<K, V> mapping in dictionary)
                    {
                        array[i++] = mapping.Value;
                    }
                }

                public int Count => dictionary.Count;

                public bool IsReadOnly => true;

                void global::System.Collections.Generic.ICollection<V>.Add(V item)
                {
                    throw new NotSupportedException(WinRTRuntimeLocalizationResources.NotSupported_ValueCollectionSet);
                }

                void global::System.Collections.Generic.ICollection<V>.Clear()
                {
                    throw new NotSupportedException(WinRTRuntimeLocalizationResources.NotSupported_ValueCollectionSet);
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
                    throw new NotSupportedException(WinRTRuntimeLocalizationResources.NotSupported_ValueCollectionSet);
                }

                IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

                public global::System.Collections.Generic.IEnumerator<V> GetEnumerator()
                {
                    return new DictionaryValueEnumerator(dictionary);
                }

                private sealed class DictionaryValueEnumerator : global::System.Collections.Generic.IEnumerator<V>
                {
                    private readonly global::System.Collections.Generic.IDictionary<K, V> dictionary;
                    private global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumeration;

                    public DictionaryValueEnumerator(global::System.Collections.Generic.IDictionary<K, V> dictionary)
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
        }

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
                    Exception e = new KeyNotFoundException(String.Format(WinRTRuntimeLocalizationResources.Arg_KeyNotFoundWithKey,key.ToString()));
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
                    Exception e = new KeyNotFoundException(String.Format(WinRTRuntimeLocalizationResources.Arg_KeyNotFoundWithKey, key.ToString()));
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

        unsafe V global::Windows.Foundation.Collections.IMap<K, V>.Lookup(K key)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __key = default;
            var __params = new object[] { ThisPtr, null, null };
            var __lookupCache = _FromMap((IWinRTObject)this)._lookupCache;
            var __hasCachedRcw = __lookupCache.TryGetValue(key, out var __cachedRcw);
            try
            {
                __key = Marshaler<K>.CreateMarshaler(key);
                __params[1] = Marshaler<K>.GetAbi(__key);
                _obj.Vftbl.Lookup_0.DynamicInvokeAbi(__params);

                if (__hasCachedRcw && __cachedRcw.Item1 == (IntPtr)__params[2])
                {
                    return __cachedRcw.Item2;
                }
                else
                {
                    var value = Marshaler<V>.FromAbi(__params[2]);
                    __lookupCache[key] = ((IntPtr)__params[2], value);
                    return value;
                }
            }
            finally
            {
                Marshaler<K>.DisposeMarshaler(__key);
                Marshaler<V>.DisposeAbi(__params[2]);
            }
        }

        unsafe bool global::Windows.Foundation.Collections.IMap<K, V>.HasKey(K key)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
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

        unsafe global::System.Collections.Generic.IReadOnlyDictionary<K, V> global::Windows.Foundation.Collections.IMap<K, V>.GetView()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
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

        unsafe bool global::Windows.Foundation.Collections.IMap<K, V>.Insert(K key, V value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __key = default;
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            _FromMap((IWinRTObject)this)._lookupCache.Remove(key);
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

        unsafe void global::Windows.Foundation.Collections.IMap<K, V>._Remove(K key)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __key = default;
            var __params = new object[] { ThisPtr, null };
            _FromMap((IWinRTObject)this)._lookupCache.Remove(key);
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

        void global::Windows.Foundation.Collections.IMap<K, V>.Clear()
        {
            _ClearHelper();
        }

        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Clear()
        {
            _ClearHelper();
        }

        private unsafe void _ClearHelper()
        {
            _FromMap((IWinRTObject)this)._lookupCache.Clear();
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_6(ThisPtr));
        }

        unsafe uint global::Windows.Foundation.Collections.IMap<K, V>.Size
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        static FromAbiHelper _FromMap(IWinRTObject obj) {
            return (FromAbiHelper)obj.GetOrCreateTypeHelperData(typeof(global::System.Collections.Generic.IDictionary<K, V>).TypeHandle,
                () => new FromAbiHelper((global::Windows.Foundation.Collections.IMap<K, V>)(IWinRTObject)obj));
        }

        global::System.Collections.Generic.ICollection<K> global::System.Collections.Generic.IDictionary<K, V>.Keys => _FromMap((IWinRTObject)this).Keys;
        global::System.Collections.Generic.ICollection<V> global::System.Collections.Generic.IDictionary<K, V>.Values => _FromMap((IWinRTObject)this).Values;
        int global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Count => _FromMap((IWinRTObject)this).Count;
        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.IsReadOnly => _FromMap((IWinRTObject)this).IsReadOnly;
        V global::System.Collections.Generic.IDictionary<K, V>.this[K key] { get => _FromMap((IWinRTObject)this)[key]; set => _FromMap((IWinRTObject)this)[key] = value; }
        void global::System.Collections.Generic.IDictionary<K, V>.Add(K key, V value) => _FromMap((IWinRTObject)this).Add(key, value);
        bool global::System.Collections.Generic.IDictionary<K, V>.ContainsKey(K key) => _FromMap((IWinRTObject)this).ContainsKey(key);
        bool global::System.Collections.Generic.IDictionary<K, V>.Remove(K key) => _FromMap((IWinRTObject)this).Remove(key);
        bool global::System.Collections.Generic.IDictionary<K, V>.TryGetValue(K key, out V value) => _FromMap((IWinRTObject)this).TryGetValue(key, out value);
        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Add(global::System.Collections.Generic.KeyValuePair<K, V> item) => _FromMap((IWinRTObject)this).Add(item);
        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Contains(global::System.Collections.Generic.KeyValuePair<K, V> item) => _FromMap((IWinRTObject)this).Contains(item);
        void global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.CopyTo(global::System.Collections.Generic.KeyValuePair<K, V>[] array, int arrayIndex) => _FromMap((IWinRTObject)this).CopyTo(array, arrayIndex);
        bool global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<K, V>>.Remove(global::System.Collections.Generic.KeyValuePair<K, V> item) => _FromMap((IWinRTObject)this).Remove(item);
        global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<K, V>>.GetEnumerator() => _FromMap((IWinRTObject)this).GetEnumerator();
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
