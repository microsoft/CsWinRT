// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using TestComponentCSharp;

namespace UnitTest;

[TestClass]
public class ArrayMarshallingE2ETests
{
    [TestMethod]
    [DataRow(2)]
    [DataRow(17)]
    [DataRow(33)]
    [DataRow(64)]
    public void NativeConstructor_FailedTypeConversionPreservesExceptionAndRecovers(int count)
    {
        var values = CreateValues(count);
        Type saved = values.Types[count / 2];
        for (int iteration = 0; iteration < 3; iteration++)
        {
            values.Types[count / 2] = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new MarshalledValuesTest(values.Numbers, values.Pairs, values.Types));

            values.Types[count / 2] = saved;
            MarshalledValuesTest instance = new(values.Numbers, values.Pairs, values.Types);
            AssertArrays(instance, values.Numbers, values.Pairs, values.Types);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(33)]
    [DataRow(64)]
    public void NativeConstructor_RetainsMappedArrayElements(int count)
    {
        var values = CreateValues(count);
        var expected = CreateValues(count);
        MarshalledValuesTest instance = new(values.Numbers, values.Pairs, values.Types);
        Array.Clear(values.Numbers);
        Array.Clear(values.Pairs);
        Array.Clear(values.Types);

        AssertArrays(instance, expected.Numbers, expected.Pairs, expected.Types);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(33)]
    [DataRow(64)]
    public void NativePropertiesAndFill_RoundTripMappedArrayElements(int count)
    {
        MarshalledValuesTest instance = new();
        var values = CreateValues(count);
        for (int iteration = 0; iteration < 3; iteration++)
        {
            instance.NullableValues = values.Numbers;
            instance.PairValues = values.Pairs;
            instance.TypeValues = values.Types;
            AssertArrays(instance, values.Numbers, values.Pairs, values.Types);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(33)]
    [DataRow(64)]
    public void ManagedPropertiesAndFill_RoundTripThroughNativeComponent(int count)
    {
        ManagedArrays target = new();
        MarshalledValuesTest proxy = new(target);
        var values = CreateValues(count);
        proxy.NullableValues = values.Numbers;
        proxy.PairValues = values.Pairs;
        proxy.TypeValues = values.Types;

        Array.Clear(values.Numbers);
        Array.Clear(values.Pairs);
        Array.Clear(values.Types);
        var expected = CreateValues(count);
        AssertArrays(proxy, expected.Numbers, expected.Pairs, expected.Types);
        Assert.AreEqual(1, target.TypeFillCalls);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(17)]
    [DataRow(33)]
    public void RcwUriArray_FailedConversionPreservesOriginalException(int count)
    {
        Uri[] values = Enumerable.Range(0, count).Select(i => new Uri($"https://example.com/{i}")).ToArray();
        values[count / 2] = new Uri("relative", UriKind.Relative);
        Class instance = new();
        Assert.ThrowsExactly<ArgumentException>(() => instance.GetAndSetUris(values));
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(17)]
    [DataRow(33)]
    [DataRow(64)]
    public void ManagedTypeFill_FailedCopyBackPreservesExceptionAndRecovers(int count)
    {
        ManagedArrays target = new() { TypeValues = CreateValues(count).Types };
        MarshalledValuesTest proxy = new(target);
        Type saved = target.TypeValues[count / 2];
        for (int iteration = 0; iteration < 3; iteration++)
        {
            target.TypeValues[count / 2] = null;
            Assert.ThrowsExactly<ArgumentNullException>(() => proxy.FillTypeValues(new Type[count]));

            target.TypeValues[count / 2] = saved;
            Type[] result = new Type[count];
            proxy.FillTypeValues(result);
            CollectionAssert.AreEqual(target.TypeValues, result);
        }
        Assert.AreEqual(6, target.TypeFillCalls);
    }

    private static (int?[] Numbers, KeyValuePair<string, string>[] Pairs, Type[] Types) CreateValues(int count) =>
        (Enumerable.Range(0, count).Select(i => i % 3 == 0 ? (int?)null : i).ToArray(),
         Enumerable.Range(0, count).Select(i => new KeyValuePair<string, string>($"key-{i}", $"value-{i}")).ToArray(),
         Enumerable.Range(0, count).Select(i => (i % 3) switch { 0 => typeof(int), 1 => typeof(string), _ => typeof(Class) }).ToArray());

    private static void AssertArrays(MarshalledValuesTest instance, int?[] numbers, KeyValuePair<string, string>[] pairs, Type[] types)
    {
        CollectionAssert.AreEqual(numbers, instance.NullableValues);
        CollectionAssert.AreEqual(pairs, instance.PairValues);
        CollectionAssert.AreEqual(types, instance.TypeValues);
        int?[] numberResult = new int?[numbers.Length];
        KeyValuePair<string, string>[] pairResult = new KeyValuePair<string, string>[pairs.Length];
        Type[] typeResult = new Type[types.Length];
        instance.FillNullableValues(numberResult);
        instance.FillPairValues(pairResult);
        instance.FillTypeValues(typeResult);
        CollectionAssert.AreEqual(numbers, numberResult);
        CollectionAssert.AreEqual(pairs, pairResult);
        CollectionAssert.AreEqual(types, typeResult);
    }

    private sealed class ManagedArrays : IArrayMarshalling
    {
        public int?[] NullableValues { get; set; } = [];
        public KeyValuePair<string, string>[] PairValues { get; set; } = [];
        public Type[] TypeValues { get; set; } = [];
        public int TypeFillCalls { get; private set; }

        public void FillNullableValues(Span<int?> values)
        {
            Assert.AreEqual(NullableValues.Length, values.Length);
            NullableValues.AsSpan().CopyTo(values);
        }

        public void FillPairValues(Span<KeyValuePair<string, string>> values)
        {
            Assert.AreEqual(PairValues.Length, values.Length);
            PairValues.AsSpan().CopyTo(values);
        }

        public void FillTypeValues(Span<Type> values)
        {
            Assert.AreEqual(TypeValues.Length, values.Length);
            TypeFillCalls++;
            TypeValues.AsSpan().CopyTo(values);
        }
    }
}
