// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from 'CSharpAnalyzerTest<TAnalyzer>' in ComputeSharp (https://github.com/Sergio0694/ComputeSharp).
// Licensed under the MIT License (MIT) (see: https://github.com/Sergio0694/ComputeSharp?tab=MIT-1-ov-file).
// Source: https://github.com/Sergio0694/ComputeSharp/blob/main/tests/ComputeSharp.Tests.SourceGenerators/Helpers/CSharpAnalyzerTest%7BTAnalyzer%7D.cs.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    /// <param name="editorconfig">The .editorconfig properties to use.</param>
    public static Task VerifyAnalyzerAsync(string source, params (string PropertyName, object PropertyValue)[] editorconfig)
    {
        CSharpAnalyzerTest<TAnalyzer> test = new(true, LanguageVersion.Latest) { TestCode = source };

        test.TestState.ReferenceAssemblies = Net10Helper.Net10;
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(ComWrappersSupport).Assembly.Location));

        // Add any editorconfig properties, if present
        if (editorconfig.Length > 0)
        {
            test.SolutionTransforms.Add((solution, projectId) =>
                solution.AddAnalyzerConfigDocument(
                    DocumentId.CreateNewId(projectId),
                    "CsWinRTSourceGeneratorTest.editorconfig",
                    SourceText.From($"""
                        is_global = true
                        {string.Join(Environment.NewLine, editorconfig.Select(static p => $"build_property.{p.PropertyName} = {p.PropertyValue}"))}
                        """,
                        Encoding.UTF8),
                filePath: "/CsWinRTSourceGeneratorTest.editorconfig"));
        }

        return test.RunAsync(CancellationToken.None);
    }
}