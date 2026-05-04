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

        // Generate an idempotent SetEntryAssembly module initializer. This ensures that when a
        // component dll is consumed in a hosted scenario where there's no natural entry assembly
        // (e.g. via WinRT.Host.dll), the dll designates itself as the entry assembly so the
        // .NET runtime's TypeMap discovery rooted at the entry assembly works. The check is
        // idempotent so that, in the multi-component aggregator scenario where 'WinRT.Component.dll'
        // loads first and sets itself as the entry assembly, this per-component initializer does
        // not override that.
        context.RegisterImplementationSourceOutput(nativeExportsInfo, Execute.EmitProjectionTypesInitializer);
    }
}