﻿// Copyright (c) Microsoft Corporation.
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
/// A diagnostic analyzer to warn for casts to <see cref="System.Runtime.InteropServices.ComImportAttribute"/> interfaces.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComImportInterfaceAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [WinRTRules.ComImportInterfaceCast];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // We only need to emit warnings if CsWinRT is in 'auto' mode (same as the collection expressions analyzer), and if the AOT analyzer is enabled.
            // This is because built-in COM is supported just fine when that is not the case, so no need to warn unless we need to be AOT compatible.
            if (!GeneratorExecutionContextHelper.IsCsWinRTAotOptimizerInAutoMode(context.Options.AnalyzerConfigOptionsProvider, context.Compilation) ||
                !GeneratorExecutionContextHelper.GetEnableAotAnalyzer(context.Options.AnalyzerConfigOptionsProvider))
            {
                return;
            }

            // This handles the following cases:
            //
            // IC c1 = (IC)obj;        
            // IC c2 = obj as IC;
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IConversionOperation { Type: INamedTypeSymbol { TypeKind: TypeKind.Interface, IsComImport: true } interfaceType })
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.ComImportInterfaceCast,
                        context.Operation.Syntax.GetLocation(),
                        interfaceType));
                }
            }, OperationKind.Conversion);

            // This handles the following cases:
            //
            // if (obj is IC)
            // {
            // }
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IIsTypeOperation { TypeOperand: INamedTypeSymbol { TypeKind: TypeKind.Interface, IsComImport: true } interfaceType })
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.ComImportInterfaceCast,
                        context.Operation.Syntax.GetLocation(),
                        interfaceType));
                }
            }, OperationKind.IsType);

            // This handles the following cases:
            //
            // if (obj is IC ic)
            // {
            // }
            //
            // List patterns are also handled:
            //
            // if (items is [IC ic, ..])
            // {
            // }
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is IDeclarationPatternOperation { MatchedType: INamedTypeSymbol { TypeKind: TypeKind.Interface, IsComImport: true } interfaceType })
                {
                    // Adjust the location for 'obj is IC ic' patterns, to include the 'is' expression as well
                    Location location = context.Operation.Parent is IIsPatternOperation isPatternOperation
                        ? isPatternOperation.Syntax.GetLocation()
                        : context.Operation.Syntax.GetLocation();

                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.ComImportInterfaceCast,
                        location,
                        interfaceType));
                }
            }, OperationKind.DeclarationPattern);

            // This handles the following cases:
            //
            // if (items is [IC, ..])
            // {
            // }
            context.RegisterOperationAction(context =>
            {
                if (context.Operation is ITypePatternOperation { MatchedType: INamedTypeSymbol { TypeKind: TypeKind.Interface, IsComImport: true } interfaceType })
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.ComImportInterfaceCast,
                        context.Operation.Syntax.GetLocation(),
                        interfaceType));
                }
            }, OperationKind.TypePattern);
        });
    }
}

#endif
