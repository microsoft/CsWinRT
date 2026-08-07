// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Basic.Reference.Assemblies;
using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinRT;

namespace SourceGeneratorTest;

[TestClass]
public class AotOptimizerTests
{
    // Regression tests for https://github.com/microsoft/CsWinRT/issues/1947. The AOT source generator has to look
    // through conditional (ternary) and switch expressions, as well as casts, to discover the concrete generic types
    // being boxed. Discovered types are registered in the generated CCW vtable lookup table, keyed by their runtime
    // 'Type.ToString()' name (eg. 'System.Collections.Generic.List`1[System.Int32]'), so their presence in the
    // generated sources proves the generator saw the boxing.

    [TestMethod]
    public void ConditionalExpression_DiscoversConcreteTypesInBothBranches()
    {
        const string source = """
            using System.Collections.Generic;

            internal class Test
            {
                private static bool GetFlag() => true;

                public object M()
                {
                    return GetFlag() ? new List<int>() : (object)new List<string>();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Generic.List`1[System.Int32]"));
        Assert.IsTrue(generated.Contains("System.Collections.Generic.List`1[System.String]"));
    }

    [TestMethod]
    public void SwitchExpression_DiscoversConcreteTypesInAllArms()
    {
        const string source = """
            using System.Collections.Generic;

            internal class Test
            {
                public object M(int selector)
                {
                    object boxed = selector switch
                    {
                        0 => new List<byte>(),
                        _ => (object)new List<float>()
                    };

                    return boxed;
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Generic.List`1[System.Byte]"));
        Assert.IsTrue(generated.Contains("System.Collections.Generic.List`1[System.Single]"));
    }

    [TestMethod]
    public void CastExpression_DiscoversConcreteOperandType()
    {
        const string source = """
            using System.Collections.Generic;

            internal class Test
            {
                public object M()
                {
                    object boxed = (object)new List<double>();

                    return boxed;
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Generic.List`1[System.Double]"));
    }

    [TestMethod]
    public void ConditionalExpression_NoBoxing_DoesNotDiscoverConcreteTypes()
    {
        // The lists are assigned to their own concrete type, so nothing is boxed or cast and there is
        // no work for the CCW lookup table generator to do. This guards against over-eager discovery.
        const string source = """
            using System.Collections.Generic;

            internal class Test
            {
                private static bool GetFlag() => true;

                public List<int> M()
                {
                    return GetFlag() ? new List<int>() : new List<int>();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsFalse(generated.Contains("System.Collections.Generic.List`1[System.Int32]"));
    }

    // Regression tests for https://github.com/microsoft/CsWinRT/issues/2507. Value types can implement WinRT
    // interfaces just like reference types do (eg. 'ImmutableArray<T>' implements 'IReadOnlyList<T>' and 'IList'),
    // so when one of them is boxed or cast, the CCW created for it also needs the vtable entries for those
    // interfaces. Types declared in the assembly being compiled get the '[WinRTExposedType]' attribute generated
    // on them (which requires them to be partial), whereas types from other assemblies, which we can't annotate,
    // go on the CCW vtable lookup table instead.

    [TestMethod]
    public void BoxedValueType_FromExternalAssembly_IsAddedToLookupTable()
    {
        const string source = """
            using System.Collections.Immutable;

            internal class Test
            {
                public object M()
                {
                    return (object)ImmutableArray.Create("a", "b");
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Immutable.ImmutableArray`1[System.String]"));

        // The bindable ('IBindableVector' and 'IBindableIterable') entries are the ones XAML needs to bind to the collection
        Assert.IsTrue(generated.Contains("global::ABI.System.Collections.IListMethods.IID"));
        Assert.IsTrue(generated.Contains("global::ABI.System.Collections.IEnumerableMethods.IID"));

        // Native callers enumerating the collection will get an enumerator adapter back, so it needs entries too
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]"));
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Object]"));
    }

    [TestMethod]
    public void BoxedValueType_FromExternalAssembly_CreatedWithCollectionExpression_IsAddedToLookupTable()
    {
        const string source = """
            using System.Collections.Immutable;

            internal class Test
            {
                public object M()
                {
                    ImmutableArray<string> array = ["a", "b"];

                    return array;
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Immutable.ImmutableArray`1[System.String]"));
    }

    [TestMethod]
    public void BoxedValueType_FromExternalAssembly_CreatedWithBuilder_IsAddedToLookupTable()
    {
        const string source = """
            using System.Collections.Immutable;

            internal class Test
            {
                public object M()
                {
                    ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

                    builder.Add("a");
                    builder.Add("b");

                    return builder.ToImmutable();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Immutable.ImmutableArray`1[System.String]"));
    }

    [TestMethod]
    public void ValueType_FromSameAssembly_GetsWinRTExposedTypeAttribute()
    {
        // Types declared in the assembly being compiled are handled by the attribute generator rather than
        // by the lookup table, so that no lookup is needed at runtime to marshal them.
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal partial struct MyCollection : IReadOnlyList<int>
            {
                public int this[int index] => 0;

                public int Count => 0;

                public IEnumerator<int> GetEnumerator() => null;

                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            internal class Test
            {
                public object M()
                {
                    return (object)new MyCollection();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("partial struct MyCollection"));
        Assert.IsFalse(generated.Contains("typeName == \"MyCollection\""));
    }

    [TestMethod]
    public void RecordType_FromSameAssembly_GetsWinRTExposedTypeAttributeWithMatchingKeyword()
    {
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal partial record MyCollection : IReadOnlyList<int>
            {
                public int this[int index] => 0;

                public int Count => 0;

                public IEnumerator<int> GetEnumerator() => null;

                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            internal class Test
            {
                public object M()
                {
                    return (object)new MyCollection();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("partial record MyCollection"));
    }

    [TestMethod]
    public void RecordStructType_FromSameAssembly_GetsWinRTExposedTypeAttributeWithMatchingKeyword()
    {
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal partial record struct MyCollection : IReadOnlyList<int>
            {
                public int this[int index] => 0;

                public int Count => 0;

                public IEnumerator<int> GetEnumerator() => null;

                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            internal class Test
            {
                public object M()
                {
                    return (object)new MyCollection();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("partial record struct MyCollection"));
    }

    [TestMethod]
    public void NestedValueType_FromSameAssembly_GetsWinRTExposedTypeAttributeWithMatchingKeywords()
    {
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal partial record Outer
            {
                internal partial struct MyCollection : IReadOnlyList<int>
                {
                    public int this[int index] => 0;

                    public int Count => 0;

                    public IEnumerator<int> GetEnumerator() => null;

                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("partial record Outer"));
        Assert.IsTrue(generated.Contains("partial struct MyCollection"));
    }

    [TestMethod]
    public void RefStructType_FromSameAssembly_IsNotProcessed()
    {
        // A 'ref struct' can never be boxed or cast to an interface, so it can never need a CCW.
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal ref partial struct MyCollection : IReadOnlyList<int>
            {
                public int this[int index] => 0;

                public int Count => 0;

                public IEnumerator<int> GetEnumerator() => null;

                IEnumerator IEnumerable.GetEnumerator() => null;
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsFalse(generated.Contains("MyCollection"));
    }

    [TestMethod]
    public void GenericValueType_FromSameAssembly_IsAddedToLookupTable()
    {
        // Generic types implementing generic WinRT interfaces can't have a single attribute generated for
        // them (each instantiation needs its own vtable), so they go on the lookup table even though they
        // are declared in the assembly being compiled.
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal partial struct MyCollection<T> : IReadOnlyList<T>
            {
                public T this[int index] => default;

                public int Count => 0;

                public IEnumerator<T> GetEnumerator() => null;

                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            internal class Test
            {
                public object M()
                {
                    return (object)new MyCollection<int>();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("typeName == \"MyCollection`1[System.Int32]\""));
    }

    [TestMethod]
    public void ValueType_NoBoxing_IsNotAddedToLookupTable()
    {
        // The immutable array is returned as its own concrete type, so it is never boxed or cast
        // and there is no work for the CCW lookup table generator to do.
        const string source = """
            using System.Collections.Immutable;

            internal class Test
            {
                public ImmutableArray<string> M()
                {
                    return ImmutableArray.Create("a", "b");
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsFalse(generated.Contains("System.Collections.Immutable.ImmutableArray`1[System.String]"));
    }

    [TestMethod]
    public void BoxedValueType_WithNoWindowsRuntimeInterfaces_IsNotAddedToLookupTable()
    {
        // The value type implements no WinRT interfaces, so there are no vtable entries to generate for it.
        const string source = """
            internal struct MyValue
            {
                public int Value;
            }

            internal class Test
            {
                public object M()
                {
                    return (object)new MyValue();
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsFalse(generated.Contains("MyValue"));
    }

    [TestMethod]
    public void BoxedProjectedValueType_IsNotAddedToLookupTable()
    {
        // 'KeyValuePair<TKey, TValue>' is a custom mapped WinRT type, so it is marshalled by the projection
        // itself rather than through the CCW vtable lookup table. This guards against over-eager discovery.
        const string source = """
            using System.Collections.Generic;

            internal class Test
            {
                public object M()
                {
                    return (object)new KeyValuePair<string, string>("a", "b");
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsFalse(generated.Contains("System.Collections.Generic.KeyValuePair`2[System.String,System.String]"));
    }

    private static string RunAotOptimizer(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        List<MetadataReference> references = new(Net80.References.All)
        {
            MetadataReference.CreateFromFile(typeof(ComWrappersSupport).Assembly.Location)
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AotOptimizerTest",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new WinRTAotSourceGenerator().AsSourceGenerator() },
            additionalTexts: ImmutableArray<AdditionalText>.Empty,
            parseOptions: (CSharpParseOptions)syntaxTree.Options,
            optionsProvider: new ConfigOptionsProvider());

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation updatedCompilation, out _);

        // Also validate the generated sources actually compile. This is what catches cases such as the generated
        // partial declaration not matching the keyword of the original one (eg. 'class' instead of 'record struct').
        Diagnostic[] errors = updatedCompilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.AreEqual(0, errors.Length, string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));

        return string.Join(
            Environment.NewLine,
            driver.GetRunResult().GeneratedTrees.Select(static tree => tree.ToString()));
    }

    private sealed class ConfigOptions : AnalyzerConfigOptions
    {
        public Dictionary<string, string> Values { get; } = new()
        {
            ["build_property.AssemblyName"] = "AotOptimizerTest",
            ["build_property.AssemblyVersion"] = "1.0.0.0",
            ["build_property.CsWinRTComponent"] = "false",
            ["build_property.CsWinRTAotOptimizerEnabled"] = "auto",
            ["build_property.CsWinRTCcwLookupTableGeneratorEnabled"] = "true",
        };

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
            return Values.TryGetValue(key, out value);
        }
    }

    private sealed class ConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new ConfigOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }
}
