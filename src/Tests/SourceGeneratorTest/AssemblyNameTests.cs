// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SourceGeneratorTest;

[TestClass]
public class AssemblyNameTests
{
    [DataTestMethod]
    [DataRow("1-HelloWorld", "_1_HelloWorld")]
    [DataRow("Example.2-Component", "Example._2_Component")]
    [DataRow("Example.Component", "Example.Component")]
    [DataRow("Example$@`?", "Example____")]
    public void ManagedExportUsesValidAssemblyNamespace(string assemblyName, string escapedName)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText("");
        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree });
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new ManagedExportGenerator() },
            additionalTexts: ImmutableArray<AdditionalText>.Empty,
            parseOptions: (CSharpParseOptions)syntaxTree.Options,
            optionsProvider: new ConfigOptionsProvider());

        driver = driver.RunGenerators(compilation);

        var generated = driver.GetRunResult().GeneratedTrees.Single();
        StringAssert.Contains(generated.ToString(), $"namespace ABI.Exports.{escapedName}");
        Assert.IsFalse(generated.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    private sealed class ManagedExportGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            ComponentGenerator.GenerateManagedDllGetActivationFactory(context);
        }
    }

    private sealed class ConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> values = new()
        {
            ["build_property.CsWinRTGenerateManagedDllGetActivationFactory"] = "true"
        };

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
            return values.TryGetValue(key, out value);
        }
    }

    private sealed class ConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new ConfigOptions();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }
}
