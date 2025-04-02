﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using WinRT;

namespace SourceGeneratorTest.Helpers;

/// <summary>
/// A custom <see cref="CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}"/> that uses a specific C# language version to parse code.
/// </summary>
/// <typeparam name="TAnalyzer">The type of the analyzer to produce diagnostics.</typeparam>
/// <typeparam name="TCodeFixer">The type of code fix to test.</typeparam>
internal sealed class CSharpCodeFixTest<TAnalyzer, TCodeFixer> : CSharpCodeFixTest<TAnalyzer, TCodeFixer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixer : CodeFixProvider, new()
{
    /// <summary>
    /// The C# language version to use to parse code.
    /// </summary>
    private readonly LanguageVersion languageVersion;

    /// <summary>
    /// Creates a new <see cref="CSharpCodeFixWithLanguageVersionTest{TAnalyzer, TCodeFix, TVerifier}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="languageVersion">The C# language version to use to parse code.</param>
    public CSharpCodeFixTest(LanguageVersion languageVersion)
    {
        this.languageVersion = languageVersion;

        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(ComWrappersSupport).Assembly.Location));
    }

    /// <inheritdoc/>
    protected override ParseOptions CreateParseOptions()
    {
        return new CSharpParseOptions(this.languageVersion, DocumentationMode.Diagnose);
    }
}
