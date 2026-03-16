// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;

namespace WindowsRuntime.SourceGenerator.Tests.Helpers;

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
    /// Creates a new <see cref="CSharpAnalyzerTest{TAnalyzer}"/> instance with the specified parameters.
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
    /// <param name="expectedDiagnostics">The list of expected diagnostic for the test (used as alternative to the markdown syntax).</param>
    /// <param name="allowUnsafeBlocks">Whether to enable unsafe blocks.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    public static Task VerifyAnalyzerAsync(
        string source,
        ReadOnlySpan<DiagnosticResult> expectedDiagnostics = default,
        bool allowUnsafeBlocks = true,
        LanguageVersion languageVersion = LanguageVersion.CSharp14)
    {
        CSharpAnalyzerTest<TAnalyzer> test = new(allowUnsafeBlocks, languageVersion) { TestCode = source };

        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net100;
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(WindowsRuntimeObject).Assembly.Location));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(CoreApplication).Assembly.Location));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Button).Assembly.Location));
        test.TestState.ExpectedDiagnostics.AddRange([.. expectedDiagnostics]);

        return test.RunAsync(CancellationToken.None);
    }
}