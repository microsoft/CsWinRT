// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from 'CSharpAnalyzerTest<TAnalyzer>' in ComputeSharp (https://github.com/Sergio0694/ComputeSharp).
// Licensed under the MIT License (MIT) (see: https://github.com/Sergio0694/ComputeSharp?tab=MIT-1-ov-file).
// Source: https://github.com/Sergio0694/ComputeSharp/blob/main/tests/ComputeSharp.Tests.SourceGenerators/Helpers/CSharpAnalyzerTest%7BTAnalyzer%7D.cs.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using WinRT;

namespace SourceGeneratorTest.Helpers;

/// <summary>
/// A custom <see cref="CSharpAnalyzerTest{TAnalyzer, TVerifier}"/> that uses a specific C# language version to parse code.
/// </summary>
/// <typeparam name="TAnalyzer">The type of the analyzer to test.</typeparam>
internal sealed class CSharpAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Whether to enable unsafe blocks.
    /// </summary>
    private readonly bool _allowUnsafeBlocks;

    /// <summary>
    /// The C# language version to use to parse code.
    /// </summary>
    private readonly LanguageVersion _languageVersion;

    /// <summary>
    /// Creates a new <see cref="CSharpAnalyzerTest{TAnalyzer}"/> instance with the specified paramaters.
    /// </summary>
    /// <param name="allowUnsafeBlocks">Whether to enable unsafe blocks.</param>
    /// <param name="languageVersion">The C# language version to use to parse code.</param>
    private CSharpAnalyzerTest(bool allowUnsafeBlocks, LanguageVersion languageVersion)
    {
        _allowUnsafeBlocks = allowUnsafeBlocks;
        _languageVersion = languageVersion;
    }

    /// <inheritdoc/>
    protected override CompilationOptions CreateCompilationOptions()
    {
        return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: _allowUnsafeBlocks);
    }

    /// <inheritdoc/>
    protected override ParseOptions CreateParseOptions()
    {
        return new CSharpParseOptions(_languageVersion, DocumentationMode.Diagnose);
    }

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync"/>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="editorconfig">The MSBuild properties (surfaced as 'build_property.*' .editorconfig entries) to use.</param>
    public static Task VerifyAnalyzerAsync(string source, params (string PropertyName, object PropertyValue)[] editorconfig)
    {
        return VerifyAnalyzerAsync(source, editorconfig, Array.Empty<(string, object)>());
    }

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync"/>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="editorconfig">The MSBuild properties (surfaced as 'build_property.*' .editorconfig entries) to use.</param>
    /// <param name="analyzerConfigOptions">The raw (non 'build_property.*') .editorconfig options to use.</param>
    public static Task VerifyAnalyzerAsync(
        string source,
        (string PropertyName, object PropertyValue)[] editorconfig,
        (string Key, object Value)[] analyzerConfigOptions)
    {
        CSharpAnalyzerTest<TAnalyzer> test = new(true, LanguageVersion.Latest) { TestCode = source };

        // Some diagnostics (eg. CsWinRT1028) are declared with multiple descriptors sharing the same id
        // (a warning and an info variant). Resolve that ambiguity in markup by using the first matching descriptor.
        test.MarkupOptions = MarkupOptions.UseFirstDescriptor;

        string winrtRuntimeAssemblyLocation = typeof(ComWrappersSupport).Assembly.Location;

        // Given we use a different nuget feed, we pass nuget.config.
        string nugetConfigFilePath = Path.Combine(Path.GetDirectoryName(winrtRuntimeAssemblyLocation), "nuget.config");

        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.WithNuGetConfigFilePath(nugetConfigFilePath);
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(winrtRuntimeAssemblyLocation));

        // Add any editorconfig properties and raw analyzer config options, if present
        if (editorconfig.Length > 0 || analyzerConfigOptions.Length > 0)
        {
            string configLines = string.Join(
                Environment.NewLine,
                editorconfig.Select(static p => $"build_property.{p.PropertyName} = {p.PropertyValue}")
                    .Concat(analyzerConfigOptions.Select(static o => $"{o.Key} = {o.Value}")));

            test.SolutionTransforms.Add((solution, projectId) =>
                solution.AddAnalyzerConfigDocument(
                    DocumentId.CreateNewId(projectId),
                    "CsWinRTSourceGeneratorTest.editorconfig",
                    SourceText.From($"""
                        is_global = true
                        {configLines}
                        """,
                        Encoding.UTF8),
                filePath: "/CsWinRTSourceGeneratorTest.editorconfig"));
        }

        return test.RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// Runs the analyzer against multiple source files, allowing scoped (non-global) .editorconfig documents
    /// to be added at specific paths so that path based .editorconfig scoping can be verified.
    /// </summary>
    /// <param name="sources">The source files (path and content, with markup) to analyze.</param>
    /// <param name="editorconfig">The MSBuild properties (surfaced as global 'build_property.*' entries) to use.</param>
    /// <param name="scopedEditorConfigs">The scoped .editorconfig documents (path and content) to add.</param>
    public static Task VerifyAnalyzerAsync(
        (string FilePath, string Content)[] sources,
        (string PropertyName, object PropertyValue)[] editorconfig,
        (string FilePath, string Content)[] scopedEditorConfigs)
    {
        CSharpAnalyzerTest<TAnalyzer> test = new(true, LanguageVersion.Latest);

        // Some diagnostics (eg. CsWinRT1028) are declared with multiple descriptors sharing the same id
        // (a warning and an info variant). Resolve that ambiguity in markup by using the first matching descriptor.
        test.MarkupOptions = MarkupOptions.UseFirstDescriptor;

        foreach (var (filePath, content) in sources)
        {
            test.TestState.Sources.Add((filePath, content));
        }

        string winrtRuntimeAssemblyLocation = typeof(ComWrappersSupport).Assembly.Location;

        // Given we use a different nuget feed, we pass nuget.config.
        string nugetConfigFilePath = Path.Combine(Path.GetDirectoryName(winrtRuntimeAssemblyLocation), "nuget.config");

        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.WithNuGetConfigFilePath(nugetConfigFilePath);
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(winrtRuntimeAssemblyLocation));

        // Add the global build properties as a global analyzer config document.
        if (editorconfig.Length > 0)
        {
            string configLines = string.Join(
                Environment.NewLine,
                editorconfig.Select(static p => $"build_property.{p.PropertyName} = {p.PropertyValue}"));

            test.SolutionTransforms.Add((solution, projectId) =>
                solution.AddAnalyzerConfigDocument(
                    DocumentId.CreateNewId(projectId),
                    "CsWinRTSourceGeneratorTest.editorconfig",
                    SourceText.From($"""
                        is_global = true
                        {configLines}
                        """,
                        Encoding.UTF8),
                filePath: "/CsWinRTSourceGeneratorTest.editorconfig"));
        }

        // Add the scoped (non-global) .editorconfig documents at their respective paths.
        foreach (var (filePath, content) in scopedEditorConfigs)
        {
            string editorConfigPath = filePath;
            string editorConfigContent = content;

            test.SolutionTransforms.Add((solution, projectId) =>
                solution.AddAnalyzerConfigDocument(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(editorConfigPath),
                    SourceText.From(editorConfigContent, Encoding.UTF8),
                    filePath: editorConfigPath));
        }

        return test.RunAsync(CancellationToken.None);
    }
}