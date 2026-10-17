// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public sealed class Test_TypeMapAssemblyTargetGenerator
{
    [TestMethod]
    public void Executable_RegistersTheExternalIidMap()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Application",
            [CSharpSyntaxTree.ParseText("public static class Program { public static void Main() { } }")],
            [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(WindowsRuntimeMarshal).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TypeMapAssemblyTargetGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation result, out _);

        string source = driver.GetRunResult().Results.Single().GeneratedSources.Single(source =>
            source.HintName == "TypeMapAssemblyTarget.Sdk.g.cs").SourceText.ToString();
        Assert.Contains(
            """[assembly: global::System.Runtime.InteropServices.TypeMapAssemblyTarget<global::WindowsRuntime.InteropServices.WindowsRuntimeIidTypeMapGroup>("WinRT.Interop")]""",
            source);
        Assert.IsFalse(result.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }
}
