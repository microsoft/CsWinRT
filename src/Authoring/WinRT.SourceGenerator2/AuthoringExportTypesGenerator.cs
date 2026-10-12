// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        // Get the name of the assembly being compiled, which the generated code is named after
        IncrementalValueProvider<string> assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? "");

        // Get the activation factories the author declared with '[WindowsRuntimeActivationFactory]'
        IncrementalValuesProvider<AuthoringActivationFactoryInfo> declaredActivationFactories = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute",
                static (node, _) => node is ClassDeclarationSyntax,
                Helpers.GetDeclaredActivationFactory)
            .SkipNullValues();

        // Get the implementations of Windows Runtime classes declared in existing metadata, which may need a factory generated
        IncrementalValuesProvider<AuthoringImplementationInfo> implementations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 },
                Helpers.GetImplementation)
            .SkipNullValues();

        // Combine them into the activation factories to emit
        IncrementalValueProvider<EquatableArray<AuthoringActivationFactoryInfo>> activationFactories = declaredActivationFactories.Collect()
            .Combine(implementations.Collect())
            .Combine(assemblyName)
            .Select(static (data, _) => Helpers.GetActivationFactories(data.Left.Left, data.Left.Right, data.Right));

        // Get the generation info specific to managed exports
        IncrementalValueProvider<AuthoringManagedExportsInfo> managedExportsInfo = context.CompilationProvider
            .Combine(options)
            .Combine(activationFactories)
            .Select(Execute.GetManagedExportsInfo);

        // Get the generation info specific to native exports
        IncrementalValueProvider<AuthoringNativeExportsInfo> nativeExportsInfo = assemblyName
            .Combine(options)
            .Combine(activationFactories)
            .Select(Execute.GetNativeExportsInfo);

        // Generate the managed exports type
        context.RegisterImplementationSourceOutput(managedExportsInfo, Execute.EmitManagedExports);

        // Generate the native exports type
        context.RegisterImplementationSourceOutput(nativeExportsInfo, Execute.EmitNativeExports);
    }
}