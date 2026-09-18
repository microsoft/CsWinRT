// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Collections;
using WindowsRuntime.InteropServices;

namespace UnitTest;

[TestClass]
public class ObservableMapTests
{
    [TestMethod]
    public void TestObservableMapDictionaryOperationsStringString()
    {
        IObservableMap<string, string> map = TestComponentCSharp.Class.CreateObservableStringMap(allowEventCalls: false);

        TestDictionaryOperations(map, "one", "first", "two", "second", "three");

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = map[null]);
        Assert.ThrowsExactly<ArgumentNullException>(() => map[null] = "value");
        Assert.ThrowsExactly<ArgumentNullException>(() => map.Add(null, "value"));
        Assert.ThrowsExactly<ArgumentNullException>(() => map.ContainsKey(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => map.Remove(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => map.TryGetValue(null, out _));
    }

    [TestMethod]
    public void TestObservableMapDictionaryOperationsIntInt()
    {
        TestDictionaryOperations(TestComponentCSharp.Class.CreateObservableIntMap(allowEventCalls: false), 1, 10, 2, 20, 3);
    }

    [TestMethod]
    public void TestObservableMapEventsStringString()
    {
        TestEvents(TestComponentCSharp.Class.CreateObservableStringMap(allowEventCalls: true), "two", "second", "third", "");
    }

    [TestMethod]
    public void TestObservableMapEventsIntInt()
    {
        TestEvents(TestComponentCSharp.Class.CreateObservableIntMap(allowEventCalls: true), 2, 20, 30, 0);
    }

    private static void TestDictionaryOperations<TKey, TValue>(
        IObservableMap<TKey, TValue> map,
        TKey firstKey,
        TValue firstValue,
        TKey secondKey,
        TValue secondValue,
        TKey missingKey)
    {
        // The native fixture rejects event calls, so the old Lookup dispatch fails with E_UNEXPECTED, not an AV.
        Assert.AreEqual(firstValue, map[firstKey]);
        TestInterfaceReferences(map);

        IDictionary<TKey, TValue> dictionary = map;
        ICollection<KeyValuePair<TKey, TValue>> collection = map;
        IReadOnlyDictionary<TKey, TValue> readOnlyDictionary = (IReadOnlyDictionary<TKey, TValue>)map;

        Assert.AreEqual(1, map.Count);
        Assert.AreEqual(1, collection.Count);
        Assert.AreEqual(1, readOnlyDictionary.Count);
        Assert.IsFalse(collection.IsReadOnly);
        Assert.AreEqual(firstValue, readOnlyDictionary[firstKey]);
        Assert.IsTrue(dictionary.ContainsKey(firstKey));
        Assert.IsFalse(dictionary.ContainsKey(missingKey));
        Assert.IsTrue(dictionary.TryGetValue(firstKey, out TValue value));
        Assert.AreEqual(firstValue, value);
        Assert.IsFalse(dictionary.TryGetValue(missingKey, out value));
        Assert.AreEqual(default(TValue), value);
        Assert.ThrowsExactly<KeyNotFoundException>(() => _ = dictionary[missingKey]);

        ICollection<TKey> keys = dictionary.Keys;
        ICollection<TValue> values = dictionary.Values;

        Assert.AreSame(keys, dictionary.Keys);
        Assert.AreSame(values, dictionary.Values);
        Assert.AreEqual(1, keys.Count);
        Assert.AreEqual(1, values.Count);
        Assert.IsTrue(keys.Contains(firstKey));
        Assert.IsFalse(keys.Contains(missingKey));
        Assert.IsTrue(values.Contains(firstValue));
        Assert.IsFalse(values.Contains(secondValue));
        CollectionAssert.AreEqual(new[] { firstKey }, readOnlyDictionary.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { firstValue }, readOnlyDictionary.Values.ToArray());

        dictionary[firstKey] = secondValue;
        Assert.AreEqual(secondValue, dictionary[firstKey]);
        dictionary.Add(secondKey, firstValue);
        Assert.ThrowsExactly<ArgumentException>(() => dictionary.Add(secondKey, secondValue));
        collection.Add(new(missingKey, secondValue));
        Assert.AreEqual(3, dictionary.Count);
        Assert.AreEqual(3, keys.Count);
        Assert.AreEqual(3, values.Count);
        Assert.IsTrue(collection.Contains(new(firstKey, secondValue)));
        Assert.IsFalse(collection.Contains(new(firstKey, firstValue)));

        KeyValuePair<TKey, TValue>[] expected =
        [
            new(firstKey, secondValue),
            new(secondKey, firstValue),
            new(missingKey, secondValue)
        ];

        CollectionAssert.AreEquivalent(expected, dictionary.ToArray());
        CollectionAssert.AreEquivalent(expected, ((IEnumerable)map).Cast<KeyValuePair<TKey, TValue>>().ToArray());

        KeyValuePair<TKey, TValue>[] copy = new KeyValuePair<TKey, TValue>[5];
        collection.CopyTo(copy, 1);
        Assert.AreEqual(default, copy[0]);
        Assert.AreEqual(default, copy[4]);
        CollectionAssert.AreEquivalent(expected, copy[1..4]);
        Assert.ThrowsExactly<ArgumentNullException>(() => collection.CopyTo(null, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => collection.CopyTo(copy, -1));
        Assert.ThrowsExactly<ArgumentException>(() => collection.CopyTo(copy, copy.Length));
        Assert.ThrowsExactly<ArgumentException>(() => collection.CopyTo(new KeyValuePair<TKey, TValue>[2], 0));

        TKey[] keyCopy = new TKey[3];
        TValue[] valueCopy = new TValue[3];
        keys.CopyTo(keyCopy, 0);
        values.CopyTo(valueCopy, 0);
        CollectionAssert.AreEquivalent(new[] { firstKey, secondKey, missingKey }, keyCopy);
        CollectionAssert.AreEquivalent(new[] { secondValue, firstValue, secondValue }, valueCopy);

        Assert.IsTrue(collection.Remove(new(missingKey, secondValue)));
        Assert.IsFalse(collection.Contains(new(missingKey, secondValue)));
        Assert.IsTrue(dictionary.Remove(secondKey));
        Assert.IsFalse(dictionary.Remove(secondKey));
        Assert.IsFalse(dictionary.TryGetValue(secondKey, out _));
        dictionary.Clear();
        Assert.AreEqual(0, dictionary.Count);
        Assert.AreEqual(0, keys.Count);
        Assert.AreEqual(0, values.Count);
        CollectionAssert.AreEqual(Array.Empty<KeyValuePair<TKey, TValue>>(), dictionary.ToArray());
        collection.CopyTo([], 0);
    }

    private static unsafe void TestInterfaceReferences<TKey, TValue>(IObservableMap<TKey, TValue> map)
    {
        using WindowsRuntimeObjectReferenceValue observableReference =
            ((IWindowsRuntimeInterface<IObservableMap<TKey, TValue>>)map).GetInterface();
        using WindowsRuntimeObjectReferenceValue dictionaryReference =
            ((IWindowsRuntimeInterface<IDictionary<TKey, TValue>>)map).GetInterface();
        using WindowsRuntimeObjectReferenceValue iterableReference =
            ((IWindowsRuntimeInterface<IEnumerable<KeyValuePair<TKey, TValue>>>)map).GetInterface();

        nint observable = (nint)observableReference.GetThisPtrUnsafe();
        nint dictionary = (nint)dictionaryReference.GetThisPtrUnsafe();
        nint iterable = (nint)iterableReference.GetThisPtrUnsafe();

        Assert.AreNotEqual(observable, dictionary);
        Assert.AreNotEqual(observable, iterable);
        Assert.AreNotEqual(dictionary, iterable);
        StringAssert.Contains(map.GetType().Name, "IObservableMap");

        Guid unknownIid = new("00000000-0000-0000-C000-000000000046");
        nint observableUnknown = 0;
        nint dictionaryUnknown = 0;
        nint iterableUnknown = 0;

        try
        {
            Assert.AreEqual(0, Marshal.QueryInterface(observable, in unknownIid, out observableUnknown));
            Assert.AreEqual(0, Marshal.QueryInterface(dictionary, in unknownIid, out dictionaryUnknown));
            Assert.AreEqual(0, Marshal.QueryInterface(iterable, in unknownIid, out iterableUnknown));
            Assert.AreEqual(observableUnknown, dictionaryUnknown);
            Assert.AreEqual(observableUnknown, iterableUnknown);
        }
        finally
        {
            WindowsRuntimeMarshal.Free((void*)iterableUnknown);
            WindowsRuntimeMarshal.Free((void*)dictionaryUnknown);
            WindowsRuntimeMarshal.Free((void*)observableUnknown);
        }

        int references = Marshal.AddRef(observable);
        _ = Marshal.Release(observable);

        using (WindowsRuntimeObjectReferenceValue roundTrip =
            ((IWindowsRuntimeInterface<IDictionary<TKey, TValue>>)map).GetInterface())
        {
            Assert.AreEqual(dictionary, (nint)roundTrip.GetThisPtrUnsafe());
        }

        int referencesAfter = Marshal.AddRef(observable);
        _ = Marshal.Release(observable);
        Assert.AreEqual(references, referencesAfter);
        GC.KeepAlive(map);
    }

    private static void TestEvents<TKey, TValue>(
        IObservableMap<TKey, TValue> map,
        TKey key,
        TValue firstValue,
        TValue secondValue,
        TKey resetKey)
    {
        List<(CollectionChange Change, TKey Key)> changes = [];
        MapChangedEventHandler<TKey, TValue> handler = (sender, args) =>
        {
            Assert.AreSame(map, sender);
            changes.Add((args.CollectionChange, args.Key));
        };

        map.MapChanged += handler;

        try
        {
            map[key] = firstValue;
            Assert.AreEqual(firstValue, map[key]);
            Assert.IsTrue(map.ContainsKey(key));
            Assert.AreEqual(2, map.Count);
            map[key] = secondValue;
            Assert.AreEqual(secondValue, map[key]);
            Assert.IsTrue(map.Remove(key));
            map.Clear();

            CollectionAssert.AreEqual(
                new[]
                {
                    (CollectionChange.ItemInserted, key),
                    // C++/WinRT also reports replacements as ItemInserted.
                    (CollectionChange.ItemInserted, key),
                    (CollectionChange.ItemRemoved, key),
                    (CollectionChange.Reset, resetKey)
                },
                changes.ToArray());
        }
        finally
        {
            map.MapChanged -= handler;
        }

        map[key] = firstValue;
        Assert.AreEqual(4, changes.Count);
    }
}
