// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that warns when an implementation of an activatable Windows Runtime class defined in existing metadata
/// leaves that class without an activation factory: none is declared with <c>[WindowsRuntimeActivationFactory]</c>, and CsWinRT
/// cannot generate one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImplementedRuntimeClassActivationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ImplementedRuntimeClassNotActivatable];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Get the '[WindowsRuntimeActivationFactory]' symbol
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute") is not { } activationFactoryAttributeType)
            {
                return;
            }

            ImplementableClassLookup implementableClasses = new();

            // The implementations of Windows Runtime classes, and the Windows Runtime classes with a declared activation factory
            ConcurrentBag<(string RuntimeClassName, INamedTypeSymbol Type, INamedTypeSymbol InstanceBase)> implementations = [];
            ConcurrentDictionary<string, byte> declaredFactories = new(StringComparer.Ordinal);

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false } type)
                {
                    return;
                }

                // Same as the source generator, which only registers declared factories deriving from a generated base
                if (type.HasAttributeWithType(activationFactoryAttributeType) &&
                    implementableClasses.GetFactoryBase(type.BaseType) is INamedTypeSymbol factoryBase)
                {
                    _ = declaredFactories.TryAdd(implementableClasses.GetFactoryRuntimeClassName(factoryBase)!, 0);
                }

                if (implementableClasses.GetInstanceBase(type.BaseType) is INamedTypeSymbol instanceBase)
                {
                    implementations.Add((implementableClasses.GetInstanceRuntimeClassName(instanceBase)!, type, instanceBase));
                }
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(context =>
            {
                foreach (IGrouping<string, (string RuntimeClassName, INamedTypeSymbol Type, INamedTypeSymbol InstanceBase)> group in implementations.GroupBy(
                    static implementation => implementation.RuntimeClassName,
                    StringComparer.Ordinal))
                {
                    // The class is activated through the factory the author declared
                    if (declaredFactories.ContainsKey(group.Key))
                    {
                        continue;
                    }

                    // All implementations of a class share the same generated bases. A class without a generated activation
                    // factory base is not activatable at all, so there is nothing to warn about.
                    if (implementableClasses.GetFactoryBaseForInstanceBase(group.First().InstanceBase) is not INamedTypeSymbol factoryBase)
                    {
                        continue;
                    }

                    // Generic implementations are never activated, so CsWinRT only considers the other ones
                    List<INamedTypeSymbol> candidates = [.. group
                        .Select(static implementation => implementation.Type)
                        .Where(static type => !type.IsGenericOrNestedInGenericType)];

                    if (candidates.Count == 0)
                    {
                        foreach ((_, INamedTypeSymbol type, _) in group)
                        {
                            Report(context, type, group.Key, factoryBase, "it is generic, or nested in a generic type, so it cannot be instantiated");
                        }

                        continue;
                    }

                    if (candidates.Count > 1)
                    {
                        string names = string.Join(", ", candidates
                            .Select(static type => $"'{type.ToDisplayString()}'")
                            .OrderBy(static name => name, StringComparer.Ordinal));

                        foreach (INamedTypeSymbol type in candidates)
                        {
                            Report(context, type, group.Key, factoryBase, $"there are several implementations of it to choose from ({names})");
                        }

                        continue;
                    }

                    INamedTypeSymbol implementation = candidates[0];

                    if (GetNotGeneratableReason(implementation, factoryBase, context.Compilation, implementableClasses) is string reason)
                    {
                        Report(context, implementation, group.Key, factoryBase, reason);
                    }
                }
            });
        });
    }

    /// <summary>
    /// Gets why CsWinRT cannot generate the activation factory for the only implementation of a Windows Runtime class.
    /// </summary>
    /// <param name="implementation">The implementation.</param>
    /// <param name="factoryBase">The generated activation factory base for the implemented class.</param>
    /// <param name="compilation">The current <see cref="Compilation"/>.</param>
    /// <param name="implementableClasses">The lookup for the generated bases.</param>
    /// <returns>The reason, or <see langword="null"/> if CsWinRT generates the activation factory.</returns>
    private static string? GetNotGeneratableReason(
        INamedTypeSymbol implementation,
        INamedTypeSymbol factoryBase,
        Compilation compilation,
        ImplementableClassLookup implementableClasses)
    {
        if (!implementableClasses.HasDefaultActivationOnly(factoryBase))
        {
            return "the class has statics or constructors with parameters, which only a declared activation factory can implement";
        }

        if (!compilation.IsSymbolAccessibleWithin(implementation, compilation.Assembly))
        {
            return $"'{implementation}' is not accessible from the rest of its assembly";
        }

        if (!implementation.InstanceConstructors.Any(constructor => constructor.Parameters.IsEmpty && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)))
        {
            return $"'{implementation}' has no parameterless constructor accessible from the rest of its assembly";
        }

        return null;
    }

    /// <summary>
    /// Reports a diagnostic for an implementation whose Windows Runtime class is not activatable.
    /// </summary>
    /// <param name="context">The <see cref="CompilationAnalysisContext"/> to report the diagnostic to.</param>
    /// <param name="type">The implementation.</param>
    /// <param name="runtimeClassName">The implemented Windows Runtime class.</param>
    /// <param name="factoryBase">The generated activation factory base for the implemented class.</param>
    /// <param name="reason">Why CsWinRT cannot generate the activation factory.</param>
    private static void Report(CompilationAnalysisContext context, INamedTypeSymbol type, string runtimeClassName, INamedTypeSymbol factoryBase, string reason)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ImplementedRuntimeClassNotActivatable,
            type.Locations.FirstOrDefault(),
            type,
            runtimeClassName,
            reason,
            factoryBase));
    }
}
