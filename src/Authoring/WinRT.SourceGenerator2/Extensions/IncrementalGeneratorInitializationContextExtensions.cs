// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extension methods for <see cref="IncrementalGeneratorInitializationContext"/>.
/// </summary>
internal static class IncrementalGeneratorInitializationContextExtensions
{
    /// <inheritdoc cref="SyntaxValueProvider.ForAttributeWithMetadataName"/>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataNameAndOptions<T>(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<GeneratorAttributeSyntaxContextWithOptions, CancellationToken, T> transform)
    {
        // Invoke 'ForAttributeWithMetadataName' normally, but just return the context directly
        IncrementalValuesProvider<GeneratorAttributeSyntaxContext> syntaxContext = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            predicate,
            static (context, token) => context);

        // Do the same for the analyzer config options
        IncrementalValueProvider<AnalyzerConfigOptions> configOptions = context.AnalyzerConfigOptionsProvider.Select(static (provider, token) => provider.GlobalOptions);

        // Merge the two and invoke the provided transform on these two values. Neither value
        // is equatable, meaning the pipeline will always re-run until this point. This is
        // intentional: we don't want any symbols or other expensive objects to be kept alive
        // across incremental steps, especially if they could cause entire compilations to be
        // rooted, which would significantly increase memory use and introduce more GC pauses.
        // In this specific case, flowing non equatable values in a pipeline is therefore fine.
        return syntaxContext.Combine(configOptions).Select((input, token) => transform(new GeneratorAttributeSyntaxContextWithOptions(input.Left, input.Right), token));
    }
}

/// <summary>
/// <inheritdoc cref="GeneratorAttributeSyntaxContext" path="/summary/node()"/>
/// </summary>
/// <param name="syntaxContext">The original <see cref="GeneratorAttributeSyntaxContext"/> value.</param>
/// <param name="globalOptions">The original <see cref="AnalyzerConfigOptions"/> value.</param>
internal readonly struct GeneratorAttributeSyntaxContextWithOptions(
    GeneratorAttributeSyntaxContext syntaxContext,
    AnalyzerConfigOptions globalOptions)
{
    /// <inheritdoc cref="GeneratorAttributeSyntaxContext.TargetNode"/>
    public SyntaxNode TargetNode { get; } = syntaxContext.TargetNode;

    /// <inheritdoc cref="GeneratorAttributeSyntaxContext.TargetSymbol"/>
    public ISymbol TargetSymbol { get; } = syntaxContext.TargetSymbol;

    /// <inheritdoc cref="GeneratorAttributeSyntaxContext.SemanticModel"/>
    public SemanticModel SemanticModel { get; } = syntaxContext.SemanticModel;

    /// <inheritdoc cref="GeneratorAttributeSyntaxContext.Attributes"/>
    public ImmutableArray<AttributeData> Attributes { get; } = syntaxContext.Attributes;

    /// <inheritdoc cref="AnalyzerConfigOptionsProvider.GlobalOptions"/>
    public AnalyzerConfigOptions GlobalOptions { get; } = globalOptions;
}