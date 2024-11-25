// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
/// int[] a = { 1, 2, 3 };
/// int[] b = new[] { 1, 2, 3 };
/// int[] c = new int[] { 1, 2, 3 };
/// </code>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionIDE0300Suppressor : DiagnosticSuppressor
{
    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [WinRTSuppressions.CollectionExpressionIDE0300];

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            // Try to get the syntax node matching the location of the diagnostic
            SyntaxNode? syntaxNode = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);

            // We only support 3 types of expressions here:
            //   Array initializer expressions: '{ 1, 2, 3 }'
            //   Implicit array creation expressions: 'new[] { 1, 2, 3 }'
            //   Array creation expressions: 'new int[] { 1, 2, 3 }'
            if (syntaxNode?.Kind() is not (SyntaxKind.ArrayInitializerExpression or SyntaxKind.ImplicitArrayCreationExpression or SyntaxKind.ArrayCreationExpression))
            {
                continue;
            }

            // If the collection expression has no elements, we want to keep the diagnostic. That is
            // because fixing the diagnostic (ie. switching to a collection expression) would be safe.
            if (syntaxNode is
                InitializerExpressionSyntax { Expressions: [] } or
                ImplicitArrayCreationExpressionSyntax { Initializer.Expressions: [] } or
                ArrayCreationExpressionSyntax { Initializer.Expressions: [] })
            {
                continue;
            }

            Microsoft.CodeAnalysis.TypeInfo typeInfo = context.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode, context.CancellationToken);

            // We might only opportunistically suppress the diagnostic when assigning to a generic interface type
            if (typeInfo.ConvertedType is not INamedTypeSymbol { TypeKind: TypeKind.Interface, IsGenericType: true, IsUnboundGenericType: false } typeSymbol)
            {
                continue;
            }

            // If the target type is either 'IEnumerable<T>', 'IReadOnlyCollection<T>', or 'IReadOnlyList<T>', suppress the diagnostic.
            // This is because using a collection expression in this case would produce an opaque type we cannot analyze for marshalling.
            if (typeSymbol.SpecialType is
                SpecialType.System_Collections_Generic_IEnumerable_T or
                SpecialType.System_Collections_Generic_IReadOnlyCollection_T or
                SpecialType.System_Collections_Generic_IReadOnlyList_T)
            {
                context.ReportSuppression(Suppression.Create(WinRTSuppressions.CollectionExpressionIDE0300, diagnostic));
            }
        }
    }
}

#endif
