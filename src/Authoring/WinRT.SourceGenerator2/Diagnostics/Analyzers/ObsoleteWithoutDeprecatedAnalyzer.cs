// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that reports when a publicly exposed API of a Windows Runtime component has an
/// <c>[Obsolete]</c> attribute applied, but no <c>[Windows.Foundation.Metadata.Deprecated]</c> attribute.
/// </summary>
/// <remarks>
/// <c>[Obsolete]</c> has no Windows Runtime counterpart, so the WinMD generator copies it verbatim rather
/// than translating it, which leaves the deprecation invisible to every consumer of the component. Having
/// both attributes applied is the supported way to deprecate an API for .NET and Windows Runtime consumers
/// alike, so that combination is not reported.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObsoleteWithoutDeprecatedAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ObsoleteWithoutDeprecated];

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

            // Get the '[Obsolete]' symbol
            if (context.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute") is not { } obsoleteAttributeType)
            {
                return;
            }

            // Get the '[Deprecated]' symbol. Without it there is no way to author the deprecation the
            // diagnostic would be suggesting, so nothing is reported (this also means the analyzer is
            // inert when the component does not reference a Windows SDK projection at all).
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.DeprecatedAttribute") is not { } deprecatedAttributeType)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                if (!IsPubliclyExposedFromComponent(context.Symbol))
                {
                    return;
                }

                // Only report APIs that are deprecated for .NET consumers, but not for Windows Runtime ones
                if (!context.Symbol.HasAttributeWithType(obsoleteAttributeType) ||
                    context.Symbol.HasAttributeWithType(deprecatedAttributeType))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ObsoleteWithoutDeprecated,
                    context.Symbol.Locations.FirstOrDefault(),
                    context.Symbol));
            }, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Event);
        });
    }

    /// <summary>
    /// Checks whether a given symbol is publicly exposed from a Windows Runtime component, and so ends up
    /// in the generated <c>.winmd</c>.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <returns>Whether <paramref name="symbol"/> is publicly exposed from the component.</returns>
    private static bool IsPubliclyExposedFromComponent(ISymbol symbol)
    {
        // Skip symbols with no declaration in source (eg. the implicit parameterless constructor of a
        // class), which cannot carry an attribute of their own and have nowhere to report a diagnostic
        if (symbol.IsImplicitlyDeclared)
        {
            return false;
        }

        if (symbol.DeclaredAccessibility is not Accessibility.Public)
        {
            return false;
        }

        // Types are only exported when they are top level: Windows Runtime has no nested types
        if (symbol is INamedTypeSymbol)
        {
            return symbol.ContainingType is null;
        }

        // Property and event accessors are only exported as part of the property or event they belong to,
        // which is the symbol reported instead. The generator moves a '[Deprecated]' from the property or
        // event down onto the accessor row itself, and never reads one written on a C# accessor.
        if (symbol is IMethodSymbol { AssociatedSymbol: not null })
        {
            return false;
        }

        // Constructors are exported as activation factory methods, and '[Deprecated]' cannot be applied to
        // them ('AttributeTargets.Constructor' is not part of its usage), so there would be no way to act
        // on the diagnostic. Deprecating the whole type is the only option, and that is reported already.
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor })
        {
            return false;
        }

        // Members are exported with their declaring type, so they are only exported when it is. Only classes
        // and interfaces export members at all: a Windows Runtime struct is a plain field aggregate, so the
        // generator drops every member of one other than its public instance fields.
        return symbol.ContainingType is { DeclaredAccessibility: Accessibility.Public, ContainingType: null, TypeKind: TypeKind.Class or TypeKind.Interface };
    }
}
