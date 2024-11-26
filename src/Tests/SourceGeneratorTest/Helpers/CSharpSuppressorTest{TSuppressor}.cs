using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using WinRT;

namespace SourceGeneratorTest.Helpers;

/// <summary>
/// A custom <see cref="CSharpAnalyzerTest{TAnalyzer, TVerifier}"/> for testing diagnostic suppressors.
/// </summary>
/// <typeparam name="TSuppressor">The type of the suppressor to test.</typeparam>
// Adapted from https://github.com/ImmediatePlatform/Immediate.Validations
public sealed class CSharpSuppressorTest<TSuppressor> : CSharpAnalyzerTest<TSuppressor, DefaultVerifier>
    where TSuppressor : DiagnosticSuppressor, new()
{
    /// <summary>
    /// The list of analyzers to run on the input code.
    /// </summary>
    private readonly List<DiagnosticAnalyzer> _analyzers = [];

    /// <summary>
    /// Whether to enable unsafe blocks.
    /// </summary>
    private readonly bool _allowUnsafeBlocks;

    /// <summary>
    /// The C# language version to use to parse code.
    /// </summary>
    private readonly LanguageVersion _languageVersion;

    /// <summary>
    /// Creates a new <see cref="CSharpSuppressorTest{TSuppressor}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="source">The source code to analyze.</param>
    /// <param name="allowUnsafeBlocks">Whether to enable unsafe blocks.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    public CSharpSuppressorTest(
        string source,
        bool allowUnsafeBlocks = true,
        LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        _allowUnsafeBlocks = allowUnsafeBlocks;
        _languageVersion = languageVersion;

        TestCode = source;
        TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(ComWrappersSupport).Assembly.Location));
    }

    /// <inheritdoc/>
    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
    {
        return base.GetDiagnosticAnalyzers().Concat(_analyzers);
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

    /// <summary>
    /// Adds a new analyzer to the set of analyzers to run on the input code.
    /// </summary>
    /// <param name="assemblyQualifiedTypeName">The type of analyzer to activate.</param>
    /// <returns>The current test instance.</returns>
    public CSharpSuppressorTest<TSuppressor> WithAnalyzer(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string assemblyQualifiedTypeName)
    {
        _analyzers.Add((DiagnosticAnalyzer)Activator.CreateInstance(Type.GetType(assemblyQualifiedTypeName)));

        return this;
    }

    /// <summary>
    /// Specifies the diagnostics to enable.
    /// </summary>
    /// <param name="diagnostics">The set of diagnostics.</param>
    /// <returns>The current test instance.</returns>
    public CSharpSuppressorTest<TSuppressor> WithSpecificDiagnostics(params DiagnosticResult[] diagnostics)
    {
        ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions = diagnostics.ToImmutableDictionary(
            descriptor => descriptor.Id,
            descriptor => descriptor.Severity.ToReportDiagnostic());

        // Transform to enable the diagnostics
        Solution EnableDiagnostics(Solution solution, ProjectId projectId)
        {
            CompilationOptions options =
                solution.GetProject(projectId)?.CompilationOptions
                ?? throw new InvalidOperationException("Compilation options missing.");

            return solution.WithProjectCompilationOptions(
                projectId,
                options.WithSpecificDiagnosticOptions(diagnosticOptions));
        }

        SolutionTransforms.Clear();
        SolutionTransforms.Add(EnableDiagnostics);

        return this;
    }

    /// <summary>
    /// Specifies the diagnostics that should be produced.
    /// </summary>
    /// <param name="diagnostics">The set of diagnostics.</param>
    /// <returns>The current test instance.</returns>
    public CSharpSuppressorTest<TSuppressor> WithExpectedDiagnosticsResults(params DiagnosticResult[] diagnostics)
    {
        ExpectedDiagnostics.AddRange(diagnostics);

        return this;
    }

    /// <summary>
    /// Specifies the .editorconfig properties to use.
    /// </summary>
    /// <param name="editorconfig">The .editorconfig properties to use.</param>
    /// <returns>The current test instance.</returns>
    public CSharpSuppressorTest<TSuppressor> WithEditorconfig(params (string PropertyName, object PropertyValue)[] editorconfig)
    {
        // Add any editorconfig properties, if present
        if (editorconfig.Length > 0)
        {
            SolutionTransforms.Add((solution, projectId) =>
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

        return this;
    }
}

/// <summary>
/// Extensions for <see cref="DiagnosticSeverity"/>.
/// </summary>
file static class DiagnosticSeverityExtensions
{
    /// <summary>
    /// Converts a <see cref="DiagnosticSeverity"/> value into a <see cref="ReportDiagnostic"/> one.
    /// </summary>
    public static ReportDiagnostic ToReportDiagnostic(this DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
            DiagnosticSeverity.Info => ReportDiagnostic.Info,
            DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
            DiagnosticSeverity.Error => ReportDiagnostic.Error,
            _ => throw new InvalidEnumArgumentException(nameof(severity), (int)severity, typeof(DiagnosticSeverity)),
        };
    }
}