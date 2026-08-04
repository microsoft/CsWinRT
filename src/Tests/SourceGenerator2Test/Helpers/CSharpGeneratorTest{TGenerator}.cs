// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;

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
    /// <param name="isCsWinRTComponent">Whether to set the <c>"CsWinRTComponent"</c> MSBuild property to <see langword="true"/>.</param>
    public static void VerifySources(string source, (string Filename, string Source) result, LanguageVersion languageVersion = LanguageVersion.CSharp14, bool isCsWinRTComponent = false)
    {
        RunGenerator(source, languageVersion, isCsWinRTComponent, out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);

        // Ensure that no diagnostics were generated
        CollectionAssert.AreEquivalent((Diagnostic[])[], diagnostics);

        // Update the assembly version using the version from the assembly of the input generators.
        // This allows the tests to not need updates whenever the version of the generators changes.
        // Also normalize line endings to 'LF', so the test files don't have to worry about that.
        string expectedText = result.Source.Replace("<ASSEMBLY_VERSION>", $"\"{typeof(TGenerator).Assembly.GetName().Version}\"").Replace("\r\n", "\n");
        string actualText = compilation.SyntaxTrees.Single(tree => Path.GetFileName(tree.FilePath) == result.Filename).ToString();

        Assert.AreEqual(expectedText, actualText);
    }

    /// <summary>
    /// Verifies that a source generator did not produce a given file.
    /// </summary>
    /// <param name="source">The input source to process.</param>
    /// <param name="filename">The name of the file that should not have been generated.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    /// <param name="isCsWinRTComponent">Whether to set the <c>"CsWinRTComponent"</c> MSBuild property to <see langword="true"/>.</param>
    public static void VerifyNoSource(string source, string filename, LanguageVersion languageVersion = LanguageVersion.CSharp14, bool isCsWinRTComponent = false)
    {
        RunGenerator(source, languageVersion, isCsWinRTComponent, out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);

        // Ensure that no diagnostics were generated
        CollectionAssert.AreEquivalent((Diagnostic[])[], diagnostics);

        Assert.IsFalse(
            compilation.SyntaxTrees.Any(tree => Path.GetFileName(tree.FilePath) == filename),
            $"The generator produced '{filename}', but it was not expected to.");
    }

    /// <summary>
    /// Gets the source generated into a given file by a source generator.
    /// </summary>
    /// <param name="source">The input source to process.</param>
    /// <param name="filename">The name of the generated file to retrieve.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    /// <param name="isCsWinRTComponent">Whether to set the <c>"CsWinRTComponent"</c> MSBuild property to <see langword="true"/>.</param>
    /// <returns>The text of the generated file.</returns>
    public static string GetGeneratedSource(string source, string filename, LanguageVersion languageVersion = LanguageVersion.CSharp14, bool isCsWinRTComponent = false)
    {
        RunGenerator(source, languageVersion, isCsWinRTComponent, out Compilation compilation, out ImmutableArray<Diagnostic> diagnostics);

        // Ensure that no diagnostics were generated
        CollectionAssert.AreEquivalent((Diagnostic[])[], diagnostics);

        return compilation.SyntaxTrees.Single(tree => Path.GetFileName(tree.FilePath) == filename).ToString();
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
            MetadataReference.CreateFromFile(typeof(CoreApplication).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Button).Assembly.Location)
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
    /// <param name="languageVersion">The language version to use to run the test.</param>
    /// <param name="isCsWinRTComponent">Whether to set the <c>"CsWinRTComponent"</c> MSBuild property to <see langword="true"/>.</param>
    /// <param name="compilation"><inheritdoc cref="GeneratorDriver.RunGeneratorsAndUpdateCompilation" path="/param[@name='outputCompilation']/node()"/></param>
    /// <param name="diagnostics"><inheritdoc cref="GeneratorDriver.RunGeneratorsAndUpdateCompilation" path="/param[@name='diagnostics']/node()"/></param>
    private static void RunGenerator(
        string source,
        LanguageVersion languageVersion,
        bool isCsWinRTComponent,
        out Compilation compilation,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        Compilation originalCompilation = CreateCompilation(source, languageVersion);

        // Create the generator driver with the D2D shader generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new TGenerator().AsSourceGenerator()],
            optionsProvider: new GlobalOptionsProvider(isCsWinRTComponent))
            .WithUpdatedParseOptions(originalCompilation.SyntaxTrees.First().Options);

        // Run all source generators on the input source code
        _ = driver.RunGeneratorsAndUpdateCompilation(originalCompilation, out compilation, out diagnostics);
    }

    /// <summary>
    /// An <see cref="AnalyzerConfigOptionsProvider"/> exposing the MSBuild properties the generators read.
    /// </summary>
    /// <param name="isCsWinRTComponent">Whether <c>"CsWinRTComponent"</c> should be set to <see langword="true"/>.</param>
    private sealed class GlobalOptionsProvider(bool isCsWinRTComponent) : AnalyzerConfigOptionsProvider
    {
        /// <inheritdoc/>
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(isCsWinRTComponent);

        /// <inheritdoc/>
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        /// <inheritdoc/>
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        /// <inheritdoc/>
        private sealed class Options(bool isCsWinRTComponent) : AnalyzerConfigOptions
        {
            /// <inheritdoc/>
            public override bool TryGetValue(string key, out string value)
            {
                if (isCsWinRTComponent && key == "build_property.CsWinRTComponent")
                {
                    value = "true";

                    return true;
                }

                value = null;

                return false;
            }
        }
    }
}