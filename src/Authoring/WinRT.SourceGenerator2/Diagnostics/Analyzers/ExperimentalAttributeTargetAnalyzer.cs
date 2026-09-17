// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that reports applications of the .NET <c>[Experimental]</c> attribute on targets that
/// Windows Runtime metadata cannot represent, in a Windows Runtime component.
/// </summary>
/// <remarks>
/// <para>
/// The Windows Runtime <c>[Experimental]</c> attribute is custom-mapped to the .NET one, so authored components
/// apply the .NET attribute and the WinMD generator translates it back (see <c>docs/attribute-projections.md</c>).
/// The .NET attribute supports more targets than the Windows Runtime one, though: it can also be applied to
/// assemblies, modules and constructors, none of which have a Windows Runtime metadata target that can carry it
/// (a constructor is exposed through an activation factory method, and no <c>.ctor</c> row in the Windows SDK
/// carries such a marker).
/// </para>
/// <para>
/// Those applications are dropped by the WinMD generator, which would silently make the API look stable to every
/// other language projection, so they are reported here instead.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExperimentalAttributeTargetAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ExperimentalAttributeUnsupportedTarget];

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

            // Get the '[Experimental]' symbol
            if (context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.ExperimentalAttribute") is not { } experimentalAttributeType)
            {
                return;
            }

            context.RegisterSyntaxNodeAction(context =>
            {
                AttributeSyntax attributeSyntax = (AttributeSyntax)context.Node;

                // Cheap syntactic filter before any semantic work: the attribute has to be named 'Experimental'
                // or 'ExperimentalAttribute' (possibly qualified) for it to possibly be the one being looked for
                if (GetUnqualifiedName(attributeSyntax.Name) is not ("Experimental" or "ExperimentalAttribute"))
                {
                    return;
                }

                if (attributeSyntax.Parent is not AttributeListSyntax attributeList)
                {
                    return;
                }

                // Classify the target syntactically, so the semantic lookup below only runs for the targets
                // that would actually be reported (the vast majority of applications are on valid targets)
                if (!TryGetUnsupportedTarget(attributeList, context.SemanticModel, context.CancellationToken, out object? target, out string? targets))
                {
                    return;
                }

                // Make sure the attribute really is the .NET '[Experimental]' one, and not just a same-named
                // attribute type declared elsewhere (the Windows Runtime one has no projected form to apply)
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol is not IMethodSymbol { ContainingType: { } attributeType } ||
                    !SymbolEqualityComparer.Default.Equals(attributeType, experimentalAttributeType))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ExperimentalAttributeUnsupportedTarget,
                    attributeSyntax.GetLocation(),
                    target,
                    targets));
            }, SyntaxKind.Attribute);
        });
    }

    /// <summary>
    /// Tries to classify the target of an attribute list as one that Windows Runtime metadata cannot represent.
    /// </summary>
    /// <param name="attributeList">The attribute list to classify.</param>
    /// <param name="semanticModel">The semantic model for the syntax tree being analyzed.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="target">The resulting message argument identifying the target, if it is unsupported.</param>
    /// <param name="targets">The resulting message argument naming the target kind in plural form, if it is unsupported.</param>
    /// <returns>Whether the target of <paramref name="attributeList"/> is one that Windows Runtime metadata cannot represent.</returns>
    private static bool TryGetUnsupportedTarget(
        AttributeListSyntax attributeList,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out object? target,
        out string? targets)
    {
        // Assembly and module level applications have no Windows Runtime metadata target at all: the generator
        // produces a fresh '.winmd' with only the authored types in it, so they are not carried over anywhere.
        if (attributeList.Target?.Identifier.Kind() is SyntaxKind.AssemblyKeyword)
        {
            target = semanticModel.Compilation.Assembly.Name;
            targets = "assemblies";

            return true;
        }

        if (attributeList.Target?.Identifier.Kind() is SyntaxKind.ModuleKeyword)
        {
            target = semanticModel.Compilation.SourceModule.Name;
            targets = "modules";

            return true;
        }

        // A constructor is exposed to Windows Runtime through an activation factory method, and the '.ctor' row
        // on the runtime class carries no marker. Only the constructors that are actually part of the component
        // surface are reported: the rest are never emitted into the '.winmd' to begin with.
        if (attributeList.Parent is ConstructorDeclarationSyntax constructorDeclaration &&
            semanticModel.GetDeclaredSymbol(constructorDeclaration, cancellationToken) is
            {
                MethodKind: MethodKind.Constructor,
                DeclaredAccessibility: Accessibility.Public
            } constructorSymbol &&
            IsExternallyVisible(constructorSymbol.ContainingType))
        {
            target = constructorSymbol;
            targets = "constructors";

            return true;
        }

        target = null;
        targets = null;

        return false;
    }

    /// <summary>
    /// Gets the unqualified name of a name syntax node (e.g. <c>Experimental</c> for <c>System.Diagnostics.CodeAnalysis.Experimental</c>).
    /// </summary>
    /// <param name="name">The name syntax node to get the unqualified name of.</param>
    /// <returns>The unqualified name of <paramref name="name"/>.</returns>
    private static string GetUnqualifiedName(NameSyntax name)
    {
        return name switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
            _ => ""
        };
    }

    /// <summary>
    /// Checks whether a given type is visible outside of the assembly declaring it.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Whether <paramref name="type"/> is visible outside of the assembly declaring it.</returns>
    private static bool IsExternallyVisible(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }
}
