// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml.Data;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_CustomPropertyProviderGenerator_GenericTypes
{
    [TestMethod]
    [DataRow("class", false)]
    [DataRow("class", true)]
    [DataRow("record", false)]
    [DataRow("record", true)]
    public void GenericProperties_ClosedOwners_PreserveTypesAndSingletons(string typeKind, bool explicitSelection)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([nameof(Value), nameof(Text), nameof(ReadOnly), nameof(Shared)], [])" : "")}}]
            public sealed partial {{typeKind}} MyType<T>
            {
                public T Value { get; set; }

                public string Text { get; set; }

                public int ReadOnly { get; init; } = 42;

                public static T Shared { get; set; }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider integers = CreateProvider(assembly, "MyNamespace.MyType`1", typeof(int));
        ICustomPropertyProvider moreIntegers = CreateProvider(assembly, "MyNamespace.MyType`1", typeof(int));
        ICustomPropertyProvider strings = CreateProvider(assembly, "MyNamespace.MyType`1", typeof(string));

        Assert.AreEqual(integers.GetType(), integers.Type);
        Assert.AreEqual(strings.GetType(), strings.Type);
        Assert.AreNotEqual(integers.Type, strings.Type);
        Assert.IsNull(integers.GetCustomProperty("Missing"));
        Assert.IsNull(integers.GetIndexedProperty("Item", typeof(int)));

        ICustomProperty integerValue = AssertWritableProperty(integers, "Value", typeof(int), 42);
        ICustomProperty stringValue = AssertWritableProperty(strings, "Value", typeof(string), "value");

        Assert.AreSame(integerValue, moreIntegers.GetCustomProperty("Value"));
        Assert.AreNotSame(integerValue, stringValue);
        Assert.AreEqual(0, integerValue.GetValue(moreIntegers));
        Assert.ThrowsExactly<InvalidCastException>(() => integerValue.SetValue(integers, "invalid"));
        Assert.ThrowsExactly<InvalidCastException>(() => integerValue.GetValue(strings));
        Assert.AreEqual(42, integerValue.GetValue(integers));

        ICustomProperty integerText = AssertWritableProperty(integers, "Text", typeof(string), "integer owner");
        ICustomProperty stringText = AssertWritableProperty(strings, "Text", typeof(string), "string owner");

        Assert.AreSame(integerText, moreIntegers.GetCustomProperty("Text"));
        Assert.AreNotSame(integerText, stringText);

        ICustomProperty readOnly = integers.GetCustomProperty("ReadOnly");

        Assert.IsNotNull(readOnly);
        Assert.AreEqual(typeof(int), readOnly.Type);
        Assert.IsTrue(readOnly.CanRead);
        Assert.IsFalse(readOnly.CanWrite);
        Assert.AreEqual(42, readOnly.GetValue(integers));
        Assert.ThrowsExactly<NotSupportedException>(() => readOnly.SetValue(integers, 100));

        ICustomProperty integerShared = integers.GetCustomProperty("Shared");
        ICustomProperty stringShared = strings.GetCustomProperty("Shared");

        Assert.IsNotNull(integerShared);
        Assert.IsNotNull(stringShared);
        Assert.AreEqual(typeof(int), integerShared.Type);
        Assert.AreEqual(typeof(string), stringShared.Type);
        Assert.IsTrue(integerShared.CanRead);
        Assert.IsTrue(integerShared.CanWrite);
        Assert.AreSame(integerShared, moreIntegers.GetCustomProperty("Shared"));
        Assert.AreNotSame(integerShared, stringShared);

        integerShared.SetValue(null, 100);
        stringShared.SetValue(new object(), "shared");

        Assert.AreEqual(100, integerShared.GetValue(null));
        Assert.AreEqual(100, integerShared.GetValue(moreIntegers));
        Assert.AreEqual("shared", stringShared.GetValue(null));
    }

    [TestMethod]
    public void GenericIndexers_MultipleParameters_PreserveIndexAndValueTypes()
    {
        const string source = """
            using System.Collections.Generic;
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public sealed partial class MyType<TKey, TValue>
                where TKey : notnull
            {
                private readonly Dictionary<TKey, TValue> values = new();
                private TValue indexedValue;

                public int Count => values.Count;

                public TValue this[TKey key]
                {
                    get => values[key];
                    set => values[key] = value;
                }

                public TValue this[int index]
                {
                    get => indexedValue;
                    set => indexedValue = value;
                }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider integers = CreateProvider(assembly, "MyNamespace.MyType`2", typeof(string), typeof(int));
        ICustomPropertyProvider moreIntegers = CreateProvider(assembly, "MyNamespace.MyType`2", typeof(string), typeof(int));
        ICustomPropertyProvider strings = CreateProvider(assembly, "MyNamespace.MyType`2", typeof(Guid), typeof(string));
        ICustomProperty integerIndexer = integers.GetIndexedProperty("Item", typeof(string));
        ICustomProperty stringIndexer = strings.GetIndexedProperty("Item", typeof(Guid));
        Guid key = Guid.NewGuid();

        Assert.IsNotNull(integerIndexer);
        Assert.IsNotNull(stringIndexer);
        Assert.AreEqual(typeof(int), integerIndexer.Type);
        Assert.AreEqual(typeof(string), stringIndexer.Type);
        Assert.AreEqual("this", integerIndexer.Name);
        Assert.IsTrue(integerIndexer.CanRead);
        Assert.IsTrue(integerIndexer.CanWrite);
        Assert.AreSame(integerIndexer, moreIntegers.GetIndexedProperty("Item", typeof(string)));
        Assert.AreNotSame(integerIndexer, stringIndexer);
        Assert.IsNull(integers.GetIndexedProperty("Item", typeof(bool)));
        Assert.IsNull(strings.GetIndexedProperty("Item", typeof(string)));

        integerIndexer.SetIndexedValue(integers, 42, "key");
        stringIndexer.SetIndexedValue(strings, "value", key);

        Assert.AreEqual(42, integerIndexer.GetIndexedValue(integers, "key"));
        Assert.AreEqual("value", stringIndexer.GetIndexedValue(strings, key));
        Assert.AreEqual(1, integers.GetCustomProperty("Count").GetValue(integers));
        Assert.AreEqual(1, strings.GetCustomProperty("Count").GetValue(strings));
        Assert.ThrowsExactly<InvalidCastException>(() => integerIndexer.GetIndexedValue(integers, key));
        Assert.ThrowsExactly<InvalidCastException>(() => integerIndexer.SetIndexedValue(integers, "invalid", "key"));
        Assert.ThrowsExactly<NotSupportedException>(() => integerIndexer.GetValue(integers));
        Assert.ThrowsExactly<NotSupportedException>(() => integerIndexer.SetValue(integers, 100));

        ICustomProperty fixedIntegerIndexer = integers.GetIndexedProperty("Item", typeof(int));
        ICustomProperty fixedStringIndexer = strings.GetIndexedProperty("Item", typeof(int));

        Assert.IsNotNull(fixedIntegerIndexer);
        Assert.IsNotNull(fixedStringIndexer);
        Assert.AreEqual(typeof(int), fixedIntegerIndexer.Type);
        Assert.AreEqual(typeof(string), fixedStringIndexer.Type);
        Assert.AreNotSame(integerIndexer, fixedIntegerIndexer);

        fixedIntegerIndexer.SetIndexedValue(integers, 100, 0);
        fixedStringIndexer.SetIndexedValue(strings, "fixed index", 0);

        Assert.AreEqual(100, fixedIntegerIndexer.GetIndexedValue(integers, 0));
        Assert.AreEqual("fixed index", fixedStringIndexer.GetIndexedValue(strings, 0));
        Assert.AreEqual(42, integerIndexer.GetIndexedValue(integers, "key"));
    }

    [TestMethod]
    public void GenericPropertySelection_ExcludesUnselectedPropertiesAndIndexers()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider([nameof(Value)], [typeof(int)])]
            public sealed partial class MyType<T>
            {
                public T Value { get; set; }

                public T Excluded { get; set; }

                public T this[int index]
                {
                    get => Value;
                    set => Value = value;
                }

                public T this[string index]
                {
                    get => Excluded;
                    set => Excluded = value;
                }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider provider = CreateProvider(assembly, "MyNamespace.MyType`1", typeof(string));
        ICustomProperty value = AssertWritableProperty(provider, "Value", typeof(string), "initial");
        ICustomProperty indexer = provider.GetIndexedProperty("Item", typeof(int));

        Assert.IsNull(provider.GetCustomProperty("Excluded"));
        Assert.IsNull(provider.GetIndexedProperty("Item", typeof(string)));
        Assert.IsNotNull(indexer);
        Assert.AreEqual(typeof(string), indexer.Type);
        Assert.AreEqual("initial", indexer.GetIndexedValue(provider, 0));

        indexer.SetIndexedValue(provider, "updated", 0);

        Assert.AreEqual("updated", value.GetValue(provider));
    }

    [TestMethod]
    [DataRow("public")]
    [DataRow("private")]
    public void NestedGenericOwners_InheritConstraints(string accessibility)
    {
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public partial class Outer<TOuter>
                where TOuter : class, IComparable<TOuter>
            {
                public partial class Middle<TItem>
                    where TItem : TOuter
                {
                    [GeneratedCustomPropertyProvider]
                    {{accessibility}} sealed partial class MyType<TKey, TValue>
                        where TKey : notnull
                        where TValue : class, ICollection<TItem>, new()
                    {
                        public TKey LastIndex;

                        public TOuter OuterValue { get; set; }

                        public TItem Current { get; set; }

                        public TValue Values { get; set; } = new();

                        public TItem this[TKey key]
                        {
                            get
                            {
                                LastIndex = key;
                                return Current;
                            }
                            set
                            {
                                LastIndex = key;
                                Current = value;
                            }
                        }
                    }
                }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider provider = CreateProvider(
            assembly,
            "MyNamespace.Outer`1+Middle`1+MyType`2",
            typeof(string), typeof(string), typeof(int), typeof(List<string>));

        Assert.AreEqual(provider.GetType(), provider.Type);
        AssertWritableProperty(provider, "OuterValue", typeof(string), "outer");
        ICustomProperty item = AssertWritableProperty(provider, "Current", typeof(string), "item");
        ICustomProperty values = provider.GetCustomProperty("Values");

        Assert.IsNotNull(values);
        Assert.IsInstanceOfType<List<string>>(values.GetValue(provider));
        AssertWritableProperty(provider, "Values", typeof(List<string>), new List<string> { "value" });

        ICustomProperty indexer = provider.GetIndexedProperty("Item", typeof(int));
        FieldInfo lastIndex = provider.GetType().GetField("LastIndex");

        Assert.IsNotNull(indexer);
        Assert.IsNotNull(lastIndex);
        Assert.AreEqual(typeof(string), indexer.Type);
        Assert.AreEqual("item", indexer.GetIndexedValue(provider, 42));
        Assert.AreEqual(42, lastIndex.GetValue(provider));

        indexer.SetIndexedValue(provider, "updated", 100);

        Assert.AreEqual(100, lastIndex.GetValue(provider));
        Assert.AreEqual("updated", item.GetValue(provider));
    }

    [TestMethod]
    public void NonGenericNestedOwner_InheritsGenericContext()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public partial class Outer<T>
                where T : unmanaged
            {
                public partial struct Middle
                {
                    [GeneratedCustomPropertyProvider]
                    private sealed partial class MyType
                    {
                        public T Value { get; set; }

                        public T? Nullable { get; set; }
                    }
                }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider integers = CreateProvider(assembly, "MyNamespace.Outer`1+Middle+MyType", typeof(int));
        ICustomPropertyProvider doubles = CreateProvider(assembly, "MyNamespace.Outer`1+Middle+MyType", typeof(double));

        Assert.AreEqual(integers.GetType(), integers.Type);
        Assert.AreEqual(doubles.GetType(), doubles.Type);
        ICustomProperty integerValue = AssertWritableProperty(integers, "Value", typeof(int), 42);
        ICustomProperty doubleValue = AssertWritableProperty(doubles, "Value", typeof(double), 3.14);
        ICustomProperty nullable = AssertWritableProperty(integers, "Nullable", typeof(int?), 100);

        Assert.AreNotSame(integerValue, doubleValue);
        Assert.AreEqual(typeof(double?), doubles.GetCustomProperty("Nullable").Type);

        nullable.SetValue(integers, null);

        Assert.IsNull(nullable.GetValue(integers));
    }

    [TestMethod]
    public void ShadowedGenericParameters_UseInnermostOwner()
    {
        const string source = """
            #pragma warning disable CS0693

            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public partial class Outer<T>
                where T : class
            {
                [GeneratedCustomPropertyProvider]
                public sealed partial class MyType<T>
                    where T : struct
                {
                    public T Value { get; set; }

                    public static T Shared { get; set; }
                }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider provider = CreateProvider(assembly, "MyNamespace.Outer`1+MyType`1", typeof(string), typeof(int));

        Assert.AreEqual(provider.GetType(), provider.Type);
        AssertWritableProperty(provider, "Value", typeof(int), 42);
        ICustomProperty shared = provider.GetCustomProperty("Shared");

        Assert.IsNotNull(shared);
        Assert.AreEqual(typeof(int), shared.Type);

        shared.SetValue(null, 100);

        Assert.AreEqual(100, shared.GetValue(null));
    }

    [TestMethod]
    [DataRow("Type")]
    [DataRow("Instance")]
    [DataRow("GetValue")]
    public void OwnerNames_MatchingDescriptorMembers_PreserveStaticAccess(string typeName)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public sealed partial class {{typeName}}
            {
                public static int Value { get; set; }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider provider = CreateProvider(assembly, $"MyNamespace.{typeName}");
        ICustomProperty property = AssertWritableProperty(provider, "Value", typeof(int), 42);

        property.SetValue(null, 100);

        Assert.AreEqual(100, property.GetValue(null));
    }

    [TestMethod]
    public void NonGenericNestedOwner_WithShadowedParameters_PreservesStaticAccess()
    {
        const string source = """
            #pragma warning disable CS0693

            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public partial class Outer<T>
                where T : struct
            {
                public partial class Middle<T>
                    where T : class
                {
                    [GeneratedCustomPropertyProvider]
                    public sealed partial class Type
                    {
                        public T Instance { get; set; }

                        public static T Shared { get; set; }
                    }
                }
            }
            """;

        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        ICustomPropertyProvider provider = CreateProvider(assembly, "MyNamespace.Outer`1+Middle`1+Type", typeof(int), typeof(string));

        AssertWritableProperty(provider, "Instance", typeof(string), "instance");
        ICustomProperty shared = provider.GetCustomProperty("Shared");

        Assert.IsNotNull(shared);
        Assert.AreEqual(typeof(string), shared.Type);

        shared.SetValue(null, "shared");

        Assert.AreEqual("shared", shared.GetValue(null));
    }

    private static ICustomPropertyProvider CreateProvider(Assembly assembly, string metadataName, params Type[] typeArguments)
    {
        Type type = assembly.GetType(metadataName, throwOnError: true);

        if (typeArguments.Length > 0)
        {
            type = type.MakeGenericType(typeArguments);
        }

        return (ICustomPropertyProvider)Activator.CreateInstance(type, nonPublic: true);
    }

    private static ICustomProperty AssertWritableProperty(ICustomPropertyProvider provider, string name, Type expectedType, object value)
    {
        ICustomProperty property = provider.GetCustomProperty(name);

        Assert.IsNotNull(property);
        Assert.AreEqual(name, property.Name);
        Assert.AreEqual(expectedType, property.Type);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);

        property.SetValue(provider, value);

        Assert.AreEqual(value, property.GetValue(provider));

        return property;
    }
}
