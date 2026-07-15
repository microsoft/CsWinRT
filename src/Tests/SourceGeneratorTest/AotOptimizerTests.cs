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
