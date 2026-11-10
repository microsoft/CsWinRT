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
/// A diagnostic analyzer that validates types annotated with <c>[WindowsRuntimeActivationFactory]</c>. The attribute supports two shapes:
/// <list type="bullet">
///   <item>A non-abstract class deriving from a generated activation factory base, implementing the activation factory of a Windows Runtime class defined in existing metadata.</item>
///   <item>A public abstract class in a Windows Runtime component, adding interfaces to the activation factory generated for an authored Windows Runtime class.</item>
/// </list>
/// It also validates implementations annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>, which request a generated
/// activation factory instead, so that a Windows Runtime class getting more than one activation factory either way is reported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WindowsRuntimeActivationFactoryAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        DiagnosticDescriptors.InvalidActivationFactoryType,
        DiagnosticDescriptors.ActivationFactoryRuntimeClassMismatch,
        DiagnosticDescriptors.DuplicateActivationFactory,
        DiagnosticDescriptors.InvalidGeneratedActivationFactoryType
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Generated code is analyzed too, without reporting diagnostics in it, as the source generator also picks up
        // activation factories declared or requested there, and a class can only have one in the whole project.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Get the '[WindowsRuntimeActivationFactory]' symbol
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute") is not { } activationFactoryAttributeType)
            {
                return;
            }

            // Get the '[GeneratedWindowsRuntimeActivationFactory]' symbol
            INamedTypeSymbol? generatedActivationFactoryAttributeType = context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.GeneratedWindowsRuntimeActivationFactoryAttribute");

            bool isComponent = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent();
            ImplementableClassLookup implementableClasses = new();

            // The valid activation factories (declared or requested), keyed by the Windows Runtime class each one is for
            ConcurrentBag<(string RuntimeClassName, INamedTypeSymbol Type, Location Location)> activationFactories = [];

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } type)
                {
                    return;
                }

                if (generatedActivationFactoryAttributeType is not null &&
                    type.TryGetAttributeWithType(generatedActivationFactoryAttributeType, out AttributeData? generatedAttribute))
                {
                    Location generatedLocation = generatedAttribute.GetLocation(context.CancellationToken) ?? type.Locations.FirstOrDefault() ?? Location.None;

                    if (ActivationFactoryValidation.GetInvalidGeneratedFactoryReason(type, context.Compilation, implementableClasses, out string? implementedRuntimeClassName, out _, out _) is string generatedInvalidReason)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.InvalidGeneratedActivationFactoryType,
                            generatedLocation,
                            type,
                            generatedInvalidReason));
                    }
                    else
                    {
                        activationFactories.Add((implementedRuntimeClassName!, type, generatedLocation));
                    }
                }

                if (!type.TryGetAttributeWithType(activationFactoryAttributeType, out AttributeData? attribute))
                {
                    return;
                }

                Location location = attribute.GetLocation(context.CancellationToken) ?? type.Locations.FirstOrDefault() ?? Location.None;

                // A factory deriving from a generated activation factory base implements a class from existing metadata,
                // whereas an abstract one extends the activation factory generated for an authored class.
                INamedTypeSymbol? factoryBase = implementableClasses.GetFactoryBase(type.BaseType);

                string? invalidReason = factoryBase is not null
                    ? ActivationFactoryValidation.GetInvalidImplementedFactoryReason(type, factoryBase, context.Compilation)
                    : ActivationFactoryValidation.GetInvalidFactoryExtensionReason(type, isComponent);

                if (invalidReason is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidActivationFactoryType,
                        location,
                        type,
                        invalidReason));

                    return;
                }

                // Skip malformed applications, eg. where the argument does not bind to a type (the compiler reports those)
                if (attribute.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol runtimeClassType }] ||
                    runtimeClassType.TypeKind is TypeKind.Error)
                {
                    return;
                }

                string runtimeClassName;
                string? mismatchReason;

                if (factoryBase is not null)
                {
                    runtimeClassName = implementableClasses.GetFactoryRuntimeClassName(factoryBase)!;
                    mismatchReason = ActivationFactoryValidation.GetImplementedRuntimeClassMismatchReason(runtimeClassType, runtimeClassName, factoryBase, implementableClasses);
                }
                else
                {
                    runtimeClassName = runtimeClassType.ToDisplayString();
                    mismatchReason = ActivationFactoryValidation.GetAuthoredRuntimeClassMismatchReason(runtimeClassType, context.Compilation, activationFactoryAttributeType, implementableClasses);
                }

                if (mismatchReason is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ActivationFactoryRuntimeClassMismatch,
                        attribute.GetArgumentLocation(0, context.CancellationToken) ?? location,
                        type,
                        runtimeClassType,
                        mismatchReason));

                    return;
                }

                activationFactories.Add((runtimeClassName, type, location));
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(context =>
            {
                foreach (IGrouping<string, (string RuntimeClassName, INamedTypeSymbol Type, Location Location)> group in activationFactories.GroupBy(
                    static factory => factory.RuntimeClassName,
                    StringComparer.Ordinal))
                {
                    if (group.Take(2).Count() < 2)
                    {
                        continue;
                    }

                    string factoryNames = string.Join(", ", group
                        .Select(static factory => $"'{factory.Type.ToDisplayString()}'")
                        .OrderBy(static name => name, StringComparer.Ordinal));

                    foreach ((_, INamedTypeSymbol type, Location location) in group)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateActivationFactory,
                            location,
                            type,
                            group.Key,
                            factoryNames));
                    }
                }
            });
        });
    }
}
