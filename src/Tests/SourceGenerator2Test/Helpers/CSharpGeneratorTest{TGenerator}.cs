// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsRuntime.SourceGenerator.Tests.Helpers;

/// <summary>
/// A helper type to run source generator tests.
/// </summary>
/// <typeparam name="TGenerator">The type of generator to test.</typeparam>
internal static class CSharpGeneratorTest<TGenerator>
    where TGenerator : IIncrementalGenerator, new()
{
    /// <summary>
    /// Verifies the resulting sources produced by a source generator.
    /// </summary>
    /// <param name="source">The input source to process.</param>
    /// <param name="result">The expected source to be generated.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    public static void VerifySources(string source, (string Filename, string Source) result, LanguageVersion languageVersion = LanguageVersion.CSharp14)
    {
        RunGenerator(source, out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, languageVersion);

        // Ensure that no diagnostics were generated
        CollectionAssert.AreEquivalent((Diagnostic[])[], diagnostics);

        // Update the assembly version using the version from the assembly of the input generators.
        // This allows the tests to not need updates whenever the version of the generators changes.
        string expectedText = result.Source.Replace("<ASSEMBLY_VERSION>", $"\"{typeof(TGenerator).Assembly.GetName().Version}\"");
        string actualText = compilation.SyntaxTrees.Single(tree => Path.GetFileName(tree.FilePath) == result.Filename).ToString();

        Assert.AreEqual(expectedText, actualText);
    }

    /// <summary>
    /// Creates a compilation from a given source.
    /// </summary>
    /// <param name="source">The input source to process.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    /// <returns>The resulting <see cref="Compilation"/> object.</returns>
    private static CSharpCompilation CreateCompilation(string source, LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        // Get all assembly references for the .NET TFM and 'WinRT.Runtime'
        IEnumerable<MetadataReference> metadataReferences =
        [
            .. Net100.References.All,
            MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location),
        ];

        // Parse the source text
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(languageVersion));

        // Create the original compilation
        return CSharpCompilation.Create(
            "original",
            [sourceTree],
            metadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    }

    /// <summary>
    /// Runs a generator and gathers the output results.
    /// </summary>
    /// <param name="source">The input source to process.</param>
    /// <param name="compilation"><inheritdoc cref="GeneratorDriver.RunGeneratorsAndUpdateCompilation" path="/param[@name='outputCompilation']/node()"/></param>
    /// <param name="diagnostics"><inheritdoc cref="GeneratorDriver.RunGeneratorsAndUpdateCompilation" path="/param[@name='diagnostics']/node()"/></param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    private static void RunGenerator(
        string source,
        out Compilation compilation,
        out ImmutableArray<Diagnostic> diagnostics,
        LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        Compilation originalCompilation = CreateCompilation(source, languageVersion);

        // Create the generator driver with the D2D shader generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TGenerator()).WithUpdatedParseOptions(originalCompilation.SyntaxTrees.First().Options);

        // Run all source generators on the input source code
        _ = driver.RunGeneratorsAndUpdateCompilation(originalCompilation, out compilation, out diagnostics);
    }
}