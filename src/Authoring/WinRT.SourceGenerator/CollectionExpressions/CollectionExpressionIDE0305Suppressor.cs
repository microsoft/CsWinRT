// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

/// <summary>
/// <para>
/// A diagnostic suppressor to suppress collection expression warnings where needed for AOT compatibility in WinRT scenarios.
/// </para>
/// <para>
/// This analyzer suppress diagnostics for cases like these:
/// <code lang="csharp">
/// List<int> i = new[] { 1, 2, 3 }.ToList();
/// IEnumerable<int> j = new[] { 1, 2, 3 }.ToList();
/// </code>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionIDE0305Suppressor : DiagnosticSuppressor
{
    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [WinRTSuppressions.CollectionExpressionIDE0305];

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            if (IsInvocationAssignedToUnsupportedInterfaceType(context, diagnostic))
            {
                context.ReportSuppression(Suppression.Create(WinRTSuppressions.CollectionExpressionIDE0305, diagnostic));
            }
        }
    }

    /// <summary>
    /// Checks whether a given diagnostic is over an invocation assigning to an unsupported interface type.
    /// </summary>
    public static bool IsInvocationAssignedToUnsupportedInterfaceType(SuppressionAnalysisContext context, Diagnostic diagnostic)
    {
        // Try to get the syntax node matching the location of the diagnostic
        SyntaxNode? syntaxNode = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);

        // We expect to have found an invocation expression (eg. 'ToList()')
        if (syntaxNode?.Kind() is not SyntaxKind.InvocationExpression)
        {
            return false;
        }

        Microsoft.CodeAnalysis.TypeInfo typeInfo = context.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode, context.CancellationToken);

        // We only want to suppress this diagnostic when the result of the invocation is assigned to an unsupported interface type
        if (typeInfo.ConvertedType is not INamedTypeSymbol { TypeKind: TypeKind.Interface, IsGenericType: true, IsUnboundGenericType: false } typeSymbol)
        {
            return false;
        }

        // Like for 'IDE0300', suppress diagnostics for 'IEnumerable<T>', 'IReadOnlyCollection<T>', or 'IReadOnlyList<T>'
        return
            typeSymbol.SpecialType is
            SpecialType.System_Collections_Generic_IEnumerable_T or
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
            SpecialType.System_Collections_Generic_IReadOnlyList_T;
    }
}
