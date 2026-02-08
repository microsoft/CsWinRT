// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using WindowsRuntime.SourceGenerator.Models;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// A generator to emit export types needing for authoring scenarios.
/// </summary>
[Generator]
public sealed partial class AuthoringExportTypesGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get the options for the generator from the analyzer options
        IncrementalValueProvider<AuthoringExportTypesOptions> options = context.AnalyzerConfigOptionsProvider.Select(Execute.GetOptions);

        // Get the generation info specific to managed exports
        IncrementalValueProvider<AuthoringManagedExportsInfo> managedExportsInfo = context.CompilationProvider
            .Combine(options)
            .Select(Execute.GetManagedExportsInfo);

        // Get the generation info specific to native exports
        IncrementalValueProvider<AuthoringNativeExportsInfo> nativeExportsInfo = context.CompilationProvider
            .Combine(options)
            .Select(Execute.GetNativeExportsInfo);

        // Generate the managed exports type
        context.RegisterImplementationSourceOutput(managedExportsInfo, Execute.EmitManagedExports);

        // Generate the native exports type
        context.RegisterImplementationSourceOutput(nativeExportsInfo, Execute.EmitNativeExports);
    }
}