// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
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

            // This handles the following cases:
            //
            // C c1 = (C)obj;        
            // C c2 = obj as C;
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IConversionOperation { Type: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } && classType.HasAttributeWithType(windowsRuntimeTypeAttribute))
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
                if (context.Operation is IIsTypeOperation { TypeOperand: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } && classType.HasAttributeWithType(windowsRuntimeTypeAttribute))
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
                if (context.Operation is IDeclarationPatternOperation { MatchedType: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } && classType.HasAttributeWithType(windowsRuntimeTypeAttribute))
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
                if (context.Operation is ITypePatternOperation { MatchedType: INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: false } classType } && classType.HasAttributeWithType(windowsRuntimeTypeAttribute))
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
