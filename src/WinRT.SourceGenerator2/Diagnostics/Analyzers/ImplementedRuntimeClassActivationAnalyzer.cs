// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that warns when an implementation of an activatable Windows Runtime class defined in existing metadata
/// leaves that class without an activation factory: none is declared for it with <c>[WindowsRuntimeActivationFactory]</c>, and
/// none is requested for it with <c>[GeneratedWindowsRuntimeActivationFactory]</c> on one of its implementations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImplementedRuntimeClassActivationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ImplementedRuntimeClassNotActivatable];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Generated code is analyzed too, without reporting diagnostics in it, as the source generator also picks up
        // activation factories declared or requested there, which make the class activatable.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Get the '[WindowsRuntimeActivationFactory]' and '[GeneratedWindowsRuntimeActivationFactory]' symbols
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute") is not { } activationFactoryAttributeType ||
                context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.GeneratedWindowsRuntimeActivationFactoryAttribute") is not { } generatedActivationFactoryAttributeType)
            {
                return;
            }

            ImplementableClassLookup implementableClasses = new();

            // The implementations of Windows Runtime classes, and the Windows Runtime classes with an activation factory
            ConcurrentBag<(string RuntimeClassName, INamedTypeSymbol Type, INamedTypeSymbol InstanceBase)> implementations = [];
            ConcurrentDictionary<string, byte> classesWithFactory = new(StringComparer.Ordinal);

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false } type)
                {
                    return;
                }

                // Valid factories count, as those are the ones the source generator registers. An invalid one counts too when
                // it is reported on its attribute, but not in generated code, where nothing would say the class is not activatable.
                bool countsInvalidFactories = !context.IsGeneratedCode;

                if (type.TryGetAttributeWithType(activationFactoryAttributeType, out AttributeData? attribute) &&
                    implementableClasses.GetFactoryBase(type.BaseType) is INamedTypeSymbol factoryBase)
                {
                    string? factoryRuntimeClassName = countsInvalidFactories
                        ? implementableClasses.GetFactoryRuntimeClassName(factoryBase)
                        : ActivationFactoryValidation.GetImplementedFactoryRuntimeClassName(type, attribute, context.Compilation, implementableClasses, out _);

                    if (factoryRuntimeClassName is not null)
                    {
                        _ = classesWithFactory.TryAdd(factoryRuntimeClassName, 0);
                    }
                }

                if (implementableClasses.GetInstanceBase(type.BaseType) is not INamedTypeSymbol instanceBase)
                {
                    return;
                }

                string runtimeClassName = implementableClasses.GetInstanceRuntimeClassName(instanceBase)!;

                if (type.HasAttributeWithType(generatedActivationFactoryAttributeType) &&
                    (countsInvalidFactories || ActivationFactoryValidation.GetInvalidGeneratedFactoryReason(type, context.Compilation, implementableClasses, out _, out _, out _) is null))
                {
                    _ = classesWithFactory.TryAdd(runtimeClassName, 0);
                }

                implementations.Add((runtimeClassName, type, instanceBase));
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(context =>
            {
                foreach (IGrouping<string, (string RuntimeClassName, INamedTypeSymbol Type, INamedTypeSymbol InstanceBase)> group in implementations.GroupBy(
                    static implementation => implementation.RuntimeClassName,
                    StringComparer.Ordinal))
                {
                    if (classesWithFactory.ContainsKey(group.Key))
                    {
                        continue;
                    }

                    // All implementations of a class share the same generated bases. A class without a generated activation
                    // factory base is not activatable at all, so there is nothing to warn about.
                    if (implementableClasses.GetFactoryBaseForInstanceBase(group.First().InstanceBase) is not INamedTypeSymbol factoryBase)
                    {
                        continue;
                    }

                    bool hasDefaultActivationOnly = implementableClasses.HasDefaultActivationOnly(factoryBase);

                    foreach ((_, INamedTypeSymbol type, _) in group)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ImplementedRuntimeClassNotActivatable,
                            type.Locations.FirstOrDefault(),
                            type,
                            group.Key,
                            GetSuggestion(type, factoryBase, hasDefaultActivationOnly, context.Compilation)));
                    }
                }
            });
        });
    }

    /// <summary>
    /// Gets how to make an implemented Windows Runtime class activatable, from one of its implementations.
    /// </summary>
    /// <param name="implementation">The implementation.</param>
    /// <param name="factoryBase">The generated activation factory base for the implemented class.</param>
    /// <param name="hasDefaultActivationOnly">Whether the implemented class is only activated through its parameterless constructor.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <returns>The suggestion to include in the diagnostic.</returns>
    private static string GetSuggestion(INamedTypeSymbol implementation, INamedTypeSymbol factoryBase, bool hasDefaultActivationOnly, Compilation compilation)
    {
        string declare = $"declare an activation factory deriving from '{factoryBase}' with '[WindowsRuntimeActivationFactory]'";

        // Only suggest the generated factory where it would be valid (otherwise, 'CSWINRT2027' would report it)
        if (hasDefaultActivationOnly && ActivationFactoryValidation.GetNotInstantiableFromGeneratedCodeReason(implementation, compilation) is null)
        {
            return $"annotate '{implementation}' with '[GeneratedWindowsRuntimeActivationFactory]', or {declare}";
        }

        return declare;
    }
}
