﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

/// <summary>
/// A diagnostic analyzer to warn on potentially trim-unsafe casts to WinRT runtime classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RuntimeClassCastAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [WinRTRules.RuntimeClassCast];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Enable the analyzer when in 'auto' mode and with the AOT analyzer enabled (same settings as the '[ComImport]' analyzer)
            if (!GeneratorExecutionContextHelper.IsCsWinRTAotOptimizerInAutoMode(context.Options.AnalyzerConfigOptionsProvider, context.Compilation) ||
                !GeneratorExecutionContextHelper.GetEnableAotAnalyzer(context.Options.AnalyzerConfigOptionsProvider))
            {
                return;
            }

            // We should always have the '[WindowsRuntimeType]' attribute, and we need it to detect projected types
            if (context.Compilation.GetTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute") is not INamedTypeSymbol windowsRuntimeTypeAttribute)
            {
                return;
            }

            // Also try to get 'System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute' if present
            INamedTypeSymbol? dynamicDependencyAttribute = context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute");

            // Helper to check a given method symbol
            bool IsDynamicDependencyPropertyForSymbol(ISymbol? symbol, INamedTypeSymbol classType)
            {
                if (symbol is null || dynamicDependencyAttribute is null)
                {
                    return false;
                }

                foreach (AttributeData attributeData in symbol.EnumerateAttributesWithType(dynamicDependencyAttribute))
                {
                    // We don't need to validate the other parameters. Using '[DynamicDependency]' is a very advanced scenario.
                    // As long as the type matches, we assume the developer knows what they're doing here, so we don't warn.
                    if (attributeData.ConstructorArguments is [_, { Kind: TypedConstantKind.Type, IsNull: false, Value: INamedTypeSymbol typeSymbol }] &&
                        SymbolEqualityComparer.Default.Equals(typeSymbol, classType))
                    {
                        return true;
                    }
                }

                return false;
            }

            // Helper to check if the containing method for a given operation has the right annotation already
            bool IsDynamicDependencyPresentForOperation(IOperation? operation, INamedTypeSymbol classType)
            {
                return operation switch
                {
                    IAnonymousFunctionOperation { Symbol: IMethodSymbol lambdaMethod } =>
                        IsDynamicDependencyPropertyForSymbol(lambdaMethod, classType) ||
                        IsDynamicDependencyPresentForOperation(operation.Parent, classType),
                    ILocalFunctionOperation { Symbol: IMethodSymbol localMethod } =>
                        IsDynamicDependencyPropertyForSymbol(localMethod, classType) ||
                        IsDynamicDependencyPresentForOperation(operation.Parent, classType),
                    IMethodBodyBaseOperation bodyOperation => IsDynamicDependencyPropertyForSymbol(operation.SemanticModel?.GetDeclaredSymbol(operation.Syntax) as IMethodSymbol, classType),
                    { } => IsDynamicDependencyPresentForOperation(operation.Parent, classType),
                    null => false
                };
            }

            // This handles the following cases:
            //
            // C c1 = (C)obj;        
            // C c2 = obj as C;
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IConversionOperation { Type: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } conversion &&
                    classType.HasAttributeWithType(windowsRuntimeTypeAttribute) &&
                    conversion.Operand is not { ConstantValue: { HasValue: true, Value: null } } &&
                    !context.Compilation.HasImplicitConversion(conversion.Operand.Type, classType) &&
                    !IsDynamicDependencyPresentForOperation(context.Operation, classType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.RuntimeClassCast,
                        context.Operation.Syntax.GetLocation(),
                        classType));
                }
            }, OperationKind.Conversion);

            // This handles the following cases:
            //
            // if (obj is C)
            // {
            // }
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IIsTypeOperation { TypeOperand: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } typeOperation &&
                    classType.HasAttributeWithType(windowsRuntimeTypeAttribute) &&
                    !context.Compilation.HasImplicitConversion(typeOperation.ValueOperand.Type, classType) &&
                    !IsDynamicDependencyPresentForOperation(context.Operation, classType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.RuntimeClassCast,
                        context.Operation.Syntax.GetLocation(),
                        classType));
                }
            }, OperationKind.IsType);

            // This handles the following cases:
            //
            // if (obj is C ic)
            // {
            // }
            //
            // List patterns are also handled:
            //
            // if (items is [C ic, ..])
            // {
            // }
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IDeclarationPatternOperation { MatchedType: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } patternOperation &&
                    classType.HasAttributeWithType(windowsRuntimeTypeAttribute) &&
                    !context.Compilation.HasImplicitConversion(patternOperation.InputType, classType) &&
                    !IsDynamicDependencyPresentForOperation(context.Operation, classType))
                {
                    // Adjust the location for 'obj is C ic' patterns, to include the 'is' expression as well
                    Location location = context.Operation.Parent is IIsPatternOperation isPatternOperation
                        ? isPatternOperation.Syntax.GetLocation()
                        : context.Operation.Syntax.GetLocation();

                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.RuntimeClassCast,
                        location,
                        classType));
                }
            }, OperationKind.DeclarationPattern);

            // This handles the following cases:
            //
            // if (items is [C, ..])
            // {
            // }
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is ITypePatternOperation { MatchedType: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } patternOperation &&
                    classType.HasAttributeWithType(windowsRuntimeTypeAttribute) &&
                    !context.Compilation.HasImplicitConversion(patternOperation.InputType, classType) &&
                    !IsDynamicDependencyPresentForOperation(context.Operation, classType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.RuntimeClassCast,
                        context.Operation.Syntax.GetLocation(),
                        classType));
                }
            }, OperationKind.TypePattern);
        });
    }
}

#endif
