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

    [TestMethod]
    public void GeneratedBindableCustomProperty_DiscoversPropertyTypes()
    {
        const string source = """
            using System.Collections.ObjectModel;
            using WinRT;

            [GeneratedBindableCustomProperty]
            internal partial class ViewModel
            {
                private readonly ObservableCollection<string> items = new();

                public ObservableCollection<string> Items
                {
                    get { return items; }
                }

                public ObservableCollection<int> Numbers { get; } = new();

                public ObservableCollection<double> Values => new();
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.ObjectModel.ObservableCollection`1[System.String]"));
        Assert.IsTrue(generated.Contains("System.Collections.ObjectModel.ObservableCollection`1[System.Int32]"));
        Assert.IsTrue(generated.Contains("System.Collections.ObjectModel.ObservableCollection`1[System.Double]"));
        Assert.IsTrue(generated.Contains("Windows.Foundation.Collections.IVector`1<String>"));
    }

    [TestMethod]
    public void NonBindableProperties_DoNotDiscoverConcreteTypes()
    {
        const string source = """
            using System.Collections.ObjectModel;

            internal class ViewModel
            {
                private readonly ObservableCollection<string> items = new();

                public ObservableCollection<string> Items
                {
                    get { return items; }
                }
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsFalse(generated.Contains("System.Collections.ObjectModel.ObservableCollection`1[System.String]"));
    }

    // Regression tests for https://github.com/microsoft/CsWinRT/issues/2537. A dictionary hands out its 'Keys' and
    // 'Values' as concrete collection types (eg. 'Dictionary<K, V>.ValueCollection') that never appear by name in
    // user code, so nothing else in the generator discovers them. Additionally, 'IReadOnlyDictionary<K, V>' (which
    // is projected as 'IMapView<K, V>') needs the same adapter types as 'IDictionary<K, V>' for iteration and for
    // 'IMapView.Split', which were previously only gathered for types implementing 'IDictionary<K, V>'.

    [TestMethod]
    public void ReadOnlyDictionary_DiscoversKeyAndValueCollections()
    {
        const string source = """
            using System.Collections.Generic;

            internal class ViewModel
            {
                public IReadOnlyDictionary<int, string> Dict { get; } = new Dictionary<int, string>();

                public static IReadOnlyDictionary<string, byte> OtherDict => new Dictionary<string, byte>();
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Generic.Dictionary`2+KeyCollection[System.Int32,System.String]"));
        Assert.IsTrue(generated.Contains("System.Collections.Generic.Dictionary`2+ValueCollection[System.Int32,System.String]"));
        Assert.IsTrue(generated.Contains("System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.Byte]"));
        Assert.IsTrue(generated.Contains("System.Collections.Generic.Dictionary`2+ValueCollection[System.String,System.Byte]"));

        // The native caller iterates those collections, so their enumerator adapters are needed as well
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Int32]"));
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]"));
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Byte]"));
    }

    [TestMethod]
    public void Dictionary_DiscoversKeyAndValueCollections()
    {
        const string source = """
            using System.Collections.Generic;

            internal class ViewModel
            {
                public IDictionary<int, string> Dict { get; } = new Dictionary<int, string>();
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("System.Collections.Generic.Dictionary`2+KeyCollection[System.Int32,System.String]"));
        Assert.IsTrue(generated.Contains("System.Collections.Generic.Dictionary`2+ValueCollection[System.Int32,System.String]"));
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Int32]"));
        Assert.IsTrue(generated.Contains("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]"));
    }

    [TestMethod]
    public void ReadOnlyDictionaryOnlyType_DiscoversIterationAndSplitAdapters()
    {
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal partial class ReadOnlyDict : IReadOnlyDictionary<int, string>
            {
                public string this[int key] => throw null;
                public IEnumerable<int> Keys => throw null;
                public IEnumerable<string> Values => throw null;
                public int Count => 0;
                public bool ContainsKey(int key) => false;
                public bool TryGetValue(int key, out string value) { value = null; return false; }
                public IEnumerator<KeyValuePair<int, string>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }

            internal class ViewModel
            {
                public object Dict { get; } = new ReadOnlyDict();
            }
            """;

        string generated = RunAotOptimizer(source);

        // 'IMapView.Split' hands out 'ConstantSplittableMap<K, V>' instances, and iterating the map hands
        // out 'KeyValuePair<K, V>' values, so both need CCW entries of their own. The assertions match the
        // lookup table keys exactly, as those bare type names also appear nested inside other keys.
        Assert.IsTrue(generated.Contains("== \"ABI.System.Collections.Generic.ConstantSplittableMap`2[System.Int32,System.String]\""));
        Assert.IsTrue(generated.Contains("== \"System.Collections.Generic.KeyValuePair`2[System.Int32,System.String]\""));
    }

    [TestMethod]
    public void CustomDictionary_DiscoversConcreteKeyAndValueCollections()
    {
        // The discovery is not specific to 'Dictionary<K, V>': any dictionary handing out its keys and values
        // through concrete collection types needs those collections on the CCW lookup table as well. The keys
        // here are typed as an interface though, so there is no concrete type to discover for them.
        const string source = """
            using System.Collections;
            using System.Collections.Generic;

            internal sealed class ValueView : IEnumerable<string>
            {
                public IEnumerator<string> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }

            internal partial class ReadOnlyDict : IReadOnlyDictionary<int, string>
            {
                public string this[int key] => throw null;
                public IEnumerable<int> Keys => throw null;
                public ValueView Values => throw null;
                IEnumerable<string> IReadOnlyDictionary<int, string>.Values => Values;
                public int Count => 0;
                public bool ContainsKey(int key) => false;
                public bool TryGetValue(int key, out string value) { value = null; return false; }
                public IEnumerator<KeyValuePair<int, string>> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }

            internal class ViewModel
            {
                public object Dict { get; } = new ReadOnlyDict();
            }
            """;

        string generated = RunAotOptimizer(source);

        Assert.IsTrue(generated.Contains("== \"ValueView\""));
        Assert.IsTrue(generated.Contains("== \"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.String]\""));

        // 'Keys' is typed as 'IEnumerable<int>', which is not an instantiable type, so nothing is registered for it
        Assert.IsFalse(generated.Contains("== \"System.Collections.Generic.IEnumerable`1[System.Int32]\""));
        Assert.IsFalse(generated.Contains("== \"ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1[System.Int32]\""));
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

        driver = driver.RunGenerators(compilation);

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
