// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that reports when a public authored type is missing a <c>[ContractVersion]</c> attribute,
/// but at least one other public type in the same compilation does have one applied. This enforces a consistent
/// versioning scheme across the public API surface of a Windows Runtime component.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PublicTypeRequiresContractVersionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.PublicTypeMissingContractVersion];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // This analyzer only applies to Windows Runtime component authoring scenarios
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent())
            {
                return;
            }

            // Get the '[ContractVersion]' symbol
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ContractVersionAttribute") is not { } contractVersionAttributeType)
            {
                return;
            }

            // Get the '[ApiContract]' symbol (used to skip API contract types, which use '[ContractVersion]'
            // with the version-only constructors to declare their own contract version, not as an association).
            INamedTypeSymbol? apiContractAttributeType = context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ApiContractAttribute");

            // Shared state across symbol actions: collect public types missing '[ContractVersion]' and track
            // whether at least one public type in the compilation does have a '[ContractVersion]' applied.
            ConcurrentBag<INamedTypeSymbol> typesMissingContractVersion = [];
            int anyTypeHasContractVersion = 0;

            context.RegisterSymbolAction(context =>
            {
                // Only consider top-level public types
                if (context.Symbol is not INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public, ContainingType: null } typeSymbol)
                {
                    return;
                }

                // Skip API contract types: those use '[ContractVersion]' with a different semantics
                // (declaring their own contract version, not associating with another contract).
                if (apiContractAttributeType is not null &&
                    typeSymbol is { TypeKind: TypeKind.Enum } &&
                    typeSymbol.HasAttributeWithType(apiContractAttributeType))
                {
                    return;
                }

                if (typeSymbol.HasAttributeWithType(contractVersionAttributeType))
                {
                    _ = Interlocked.Exchange(ref anyTypeHasContractVersion, 1);
                }
                else
                {
                    typesMissingContractVersion.Add(typeSymbol);
                }
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(context =>
            {
                // Only report if at least one public type does have a '[ContractVersion]' applied: the
                // analyzer specifically targets components that have opted into contract versioning, but
                // are inconsistently applying it across the public API surface.
                if (Volatile.Read(ref anyTypeHasContractVersion) == 0)
                {
                    return;
                }

                // Sort by source location for deterministic diagnostic reporting order
                foreach (INamedTypeSymbol typeSymbol in typesMissingContractVersion
                    .OrderBy(static t => t.Locations.FirstOrDefault()?.SourceTree?.FilePath, StringComparer.Ordinal)
                    .ThenBy(static t => t.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.PublicTypeMissingContractVersion,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol));
                }
            });
        });
    }
}
