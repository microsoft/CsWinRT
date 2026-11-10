// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using WindowsRuntime.SourceGenerator.Models;

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
        // Gather the info on all types annotated with '[GeneratedCustomPropertyProvider]'.
        IncrementalValuesProvider<CustomPropertyProviderInfo> providerInfo = context.ForAttributeWithMetadataNameAndOptions(
            fullyQualifiedMetadataName: "WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute",
            predicate: Execute.IsTargetNodeValid,
            transform: Execute.GetCustomPropertyProviderInfo)
            .WithTrackingName("CustomPropertyProviderInfo")
            .SkipNullValues();

        // Write the implementation for all annotated types
        context.RegisterSourceOutput(providerInfo, Emit.WriteCustomPropertyProviderImplementation);
    }
}