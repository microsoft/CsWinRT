using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using WinRT;

namespace SourceGeneratorTest.Helpers;

/// <summary>
/// A custom <see cref="CSharpAnalyzerTest{TAnalyzer, TVerifier}"/> that uses a specific C# language version to parse code.
/// </summary>
/// <typeparam name="TAnalyzer">The type of the analyzer to test.</typeparam>
// Ported from https://github.com/Sergio0694/ComputeSharp
internal sealed class CSharpAnalyzerWithLanguageVersionTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
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
    /// Creates a new <see cref="CSharpAnalyzerWithLanguageVersionTest{TAnalyzer}"/> instance with the specified paramaters.
    /// </summary>
    /// <param name="allowUnsafeBlocks">Whether to enable unsafe blocks.</param>
    /// <param name="languageVersion">The C# language version to use to parse code.</param>
    private CSharpAnalyzerWithLanguageVersionTest(bool allowUnsafeBlocks, LanguageVersion languageVersion)
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
    /// <param name="allowUnsafeBlocks">Whether to enable unsafe blocks.</param>
    /// <param name="languageVersion">The language version to use to run the test.</param>
    public static Task VerifyAnalyzerAsync(
        string source,
        bool allowUnsafeBlocks = true,
        LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        CSharpAnalyzerWithLanguageVersionTest<TAnalyzer> test = new(allowUnsafeBlocks, languageVersion) { TestCode = source };

        test.TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(ComWrappersSupport).Assembly.Location));

        return test.RunAsync(CancellationToken.None);
    }
}