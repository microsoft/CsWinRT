// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// Validates exclusions from an authored component's Windows Runtime metadata.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExcludeFromWindowsMetadataAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.InvalidExcludeFromWindowsMetadata,
        DiagnosticDescriptors.IgnoredTypeInExportedSignature
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        // Generated declarations also contribute to the compiled component's exported API
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent())
            {
                return;
            }

            AuthoringTypeValidation validation = new(
                context.Compilation,
                context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTUseWindowsUIXamlProjections());
            INamedTypeSymbol? excludeAttributeType = validation.ExcludeAttributeType;

            // Validate accessor-level applications at the attribute syntax rather than scanning every member's attributes
            if (excludeAttributeType is not null)
            {
                context.RegisterSyntaxNodeAction(context =>
                {
                    AttributeSyntax attribute = (AttributeSyntax)context.Node;

                    // Filter by the target first, then bind the attribute constructor to recognize qualified names and aliases
                    if (attribute.Parent?.Parent is not AccessorDeclarationSyntax accessor ||
                        context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor ||
                        !SymbolEqualityComparer.Default.Equals(constructor.ContainingType, excludeAttributeType))
                    {
                        return;
                    }

                    // The guard above identified an accessor; report its invalid annotation only if the containing type is exported
                    // Excluding the whole property or event does not make an accessor-level annotation valid
                    if (context.SemanticModel.GetDeclaredSymbol(accessor, context.CancellationToken) is IMethodSymbol method &&
                        validation.IsExported(method.ContainingType))
                    {
                        context.ReportDiagnostic(CreateInvalidExclusionDiagnostic(
                            method, "apply the attribute to the whole property or event, not an accessor"));
                    }
                }, SyntaxKind.Attribute);
            }

            // Exported signatures and implicit fields also need validation when they have no attribute syntax to inspect
            context.RegisterSymbolAction(context =>
            {
                INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

                if (!validation.IsExported(type))
                {
                    return;
                }

                CheckAttributes(context, type, validation);

                if (type is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke })
                {
                    // Validate the delegate's declared return type and parameters through its 'Invoke' method
                    CheckSignature(context, type, invoke.ReturnType, validation);

                    foreach (IParameterSymbol parameter in invoke.Parameters)
                    {
                        CheckSignature(context, parameter, parameter.Type, validation);
                    }

                    return;
                }

                // Abstract and excluded managed bases are replaced with 'System.Object' during WinMD emission
                if (type.BaseType is { IsAbstract: false } baseType &&
                    !baseType.IsExcludedFromWindowsMetadata(excludeAttributeType))
                {
                    CheckSignature(context, type, baseType, validation);
                }

                ImmutableArray<INamedTypeSymbol> projectedInterfaces =
                    [.. validation.EnumerateProjectedInterfaces(type)];

                foreach (INamedTypeSymbol iface in validation.EnumerateProjectedInterfaceDeclarations(type)
                    .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
                {
                    if (!type.Interfaces.Contains(iface, SymbolEqualityComparer.Default) && !validation.IsMapped(iface))
                    {
                        continue;
                    }

                    // Classes can omit excluded implemented interfaces, but exported interfaces must retain valid base contracts
                    if (!AuthoringTypeValidation.IsOmittedInterface(iface) &&
                        (type.TypeKind is TypeKind.Interface ||
                         (iface.IsPubliclyAccessible &&
                          !iface.IsExcludedFromWindowsMetadata(excludeAttributeType))))
                    {
                        CheckSignature(context, type, iface, validation);
                    }
                }

                foreach (ISymbol member in type.GetMembers())
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    // Inspect the field symbols so field-targeted attributes on generated backing fields are covered too
                    if (member is IFieldSymbol { IsStatic: false } structField &&
                        type.TypeKind is TypeKind.Struct &&
                        structField.IsExcludedFromWindowsMetadata(excludeAttributeType))
                    {
                        context.ReportDiagnostic(CreateInvalidExclusionDiagnostic(
                            structField, "instance fields of exported structs define the Windows Runtime layout"));
                    }

                    if (member.IsExcludedFromWindowsMetadata(excludeAttributeType))
                    {
                        // Excluding a member must not break an interface that remains exposed
                        ValidateRequiredMember(context, type, member, validation, projectedInterfaces);
                        continue;
                    }

                    if (member.DeclaredAccessibility is not Accessibility.Public || IsOmittedMember(type, member, validation, projectedInterfaces))
                    {
                        continue;
                    }

                    // References in managed-only members do not need exported metadata types
                    bool emitsMetadata =
                        (type.TypeKind is TypeKind.Struct && member is IFieldSymbol { IsStatic: false }) ||
                        (type.TypeKind is TypeKind.Enum && member is IFieldSymbol { IsConst: true }) ||
                        (type.TypeKind is TypeKind.Class or TypeKind.Interface &&
                         member is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } or
                             IPropertySymbol or IEventSymbol);

                    if (!emitsMetadata)
                    {
                        continue;
                    }

                    CheckAttributes(context, member, validation);

                    switch (member)
                    {
                        case IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor or MethodKind.DelegateInvoke } method:
                            CheckSignature(context, method, method.ReturnType, validation);

                            foreach (IParameterSymbol parameter in method.Parameters)
                            {
                                CheckSignature(context, parameter, parameter.Type, validation);
                            }

                            break;
                        case IPropertySymbol property:
                            CheckSignature(context, property, property.Type, validation);

                            foreach (IParameterSymbol parameter in property.Parameters)
                            {
                                CheckSignature(context, parameter, parameter.Type, validation);
                            }

                            break;
                        case IEventSymbol @event:
                            CheckSignature(context, @event, @event.Type, validation);
                            break;
                        case IFieldSymbol { IsStatic: false } field when type.TypeKind is TypeKind.Struct:
                            CheckSignature(context, field, field.Type, validation);
                            break;
                        default:
                            break;
                    }
                }
            }, SymbolKind.NamedType);
        });
    }

    /// <summary>
    /// Checks a signature recursively for excluded types.
    /// </summary>
    private static void CheckSignature(
        SymbolAnalysisContext context,
        ISymbol declaration,
        ITypeSymbol type,
        AuthoringTypeValidation validation)
    {
        if (validation.FindIgnoredType(type) is { } ignored)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.IgnoredTypeInExportedSignature,
                declaration.Locations.FirstOrDefault(),
                declaration,
                ignored));
        }
    }

    /// <summary>
    /// Checks copied attribute arguments for type references that would be missing from the WinMD.
    /// </summary>
    private static void CheckAttributes(
        SymbolAnalysisContext context,
        ISymbol declaration,
        AuthoringTypeValidation validation)
    {
        void CheckArgument(TypedConstant argument)
        {
            if (argument.Kind is TypedConstantKind.Type && argument.Value is ITypeSymbol type)
            {
                CheckSignature(context, declaration, type, validation);
            }
            else if (argument.Kind is TypedConstantKind.Array && !argument.IsNull)
            {
                foreach (TypedConstant element in argument.Values)
                {
                    CheckArgument(element);
                }
            }
        }

        foreach (AttributeData attribute in declaration.GetAttributes())
        {
            // Attributes omitted from the WinMD cannot leave references to excluded types
            if (attribute.AttributeClass is not { DeclaredAccessibility: Accessibility.Public } attributeType ||
                attributeType.IsExcludedFromWindowsMetadata(validation.ExcludeAttributeType) ||
                attributeType.GetFullyQualifiedMetadataName().StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (TypedConstant argument in attribute.ConstructorArguments)
            {
                CheckArgument(argument);
            }

            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                CheckArgument(argument.Value);
            }
        }
    }

    /// <summary>
    /// Gets whether an interface mapping removes a member from the authored surface.
    /// </summary>
    private static bool IsOmittedMember(
        INamedTypeSymbol type,
        ISymbol member,
        AuthoringTypeValidation validation,
        ImmutableArray<INamedTypeSymbol> projectedInterfaces)
    {
        return projectedInterfaces.Any(iface =>
            (validation.IsMapped(iface) || AuthoringTypeValidation.IsOmittedInterface(iface)) &&
            iface.GetMembers().Any(contract =>
                SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(contract), member)));
    }

    /// <summary>
    /// Rejects exclusions of members still needed by a retained interface.
    /// </summary>
    private static void ValidateRequiredMember(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        ISymbol member,
        AuthoringTypeValidation validation,
        ImmutableArray<INamedTypeSymbol> projectedInterfaces)
    {
        if (type.TypeKind is not TypeKind.Class || member is IMethodSymbol { AssociatedSymbol: not null })
        {
            return;
        }

        foreach (INamedTypeSymbol iface in projectedInterfaces)
        {
            // An omitted base interface can still supply required members of a mapped contract
            // For example, 'ICollection<T>.Count' remains required by an exported 'IList<T>'
            if (!iface.IsPubliclyAccessible ||
                iface.IsExcludedFromWindowsMetadata(validation.ExcludeAttributeType) ||
                (AuthoringTypeValidation.IsOmittedInterface(iface) &&
                 !projectedInterfaces.Any(mapped =>
                     validation.IsMapped(mapped) &&
                     mapped.AllInterfaces.Contains(iface, SymbolEqualityComparer.Default))))
            {
                continue;
            }

            if (iface.GetMembers().Any(contract =>
                !contract.IsStatic &&
                !contract.IsExcludedFromWindowsMetadata(validation.ExcludeAttributeType) &&
                SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(contract), member)))
            {
                context.ReportDiagnostic(CreateInvalidExclusionDiagnostic(
                    member, $"the member is required by retained interface '{iface}'"));

                return;
            }
        }
    }

    /// <summary>
    /// Creates a diagnostic for an exclusion that cannot be represented without breaking a contract.
    /// </summary>
    private static Diagnostic CreateInvalidExclusionDiagnostic(
        ISymbol declaration,
        string reason)
    {
        return Diagnostic.Create(
            DiagnosticDescriptors.InvalidExcludeFromWindowsMetadata,
            declaration.Locations.FirstOrDefault(),
            declaration,
            reason);
    }
}