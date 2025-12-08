// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// A generator to emit <c>ICustomPropertyProvider</c> implementations for annotated types.
/// </summary>
[Generator]
public sealed partial class CustomPropertyProviderGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var bindableCustomPropertyAttributes = context.ForAttributeWithMetadataNameAndOptions(
            fullyQualifiedMetadataName: "WinRT.GeneratedBindableCustomPropertyAttribute",
            predicate: Execute.IsTargetNodeValid,
            transform: static (n, _) => n)
        .Combine(properties)
        .Select(static ((GeneratorAttributeSyntaxContext generatorSyntaxContext, CsWinRTAotOptimizerProperties properties) value, CancellationToken _) =>
                value.properties.IsCsWinRTAotOptimizerEnabled ? GetBindableCustomProperties(value.generatorSyntaxContext) : default)
        .Where(static bindableCustomProperties => bindableCustomProperties != default)
        .Collect()
        .Combine(properties);
        context.RegisterImplementationSourceOutput(bindableCustomPropertyAttributes, GenerateBindableCustomProperties);
    }
}